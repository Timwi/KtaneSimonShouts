using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using SimonShouts;
using UnityEngine;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Simon Shouts
/// Created by Timwi
/// </summary>
public class SimonShoutsModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    public MeshRenderer[] Squares;
    public Material[] SquareMaterials;
    public Material GraySquareMaterial;
    public GameObject[] ButtonLights;
    public KMSelectable[] Buttons;
    public MeshRenderer[] ButtonRenderers;
    public Material[] ButtonMaterials;

    private static readonly string _grid = @"RRGRRRGRYRBRYRBRRRRGRBRYRGRRRBRYRGGGRGGGYGBGYGBGGRGGGBGYGGGRGBGYRRGRRRGRYRBRYRBRBRBGBBBYBGBRBBBYRGGGRGGGYGBGYGBGYRYGYBYYYGYRYBYYRYGYRYGYYYBYYYBYGRGGGBGYGGGRGBGYRBGBRBGBYBBBYBBBRRRGRBRYRGRRRBRYRYGYRYGYYYBYYYBYBRBGBBBYBGBRBBBYRBGBRBGBYBBBYBBBYRYGYBYYYGYRYBYY";
    private static readonly bool[][] _morse = "# ###,### # # #,### # ### #,### # #,#,# # ### #,### ### #,# # # #,# #,# ### ### ###,### # ###,# ### # #,### ###,### #,### ### ###,# ### ### #,### ### # ###,# ### #,# # #,###,# # ###,# # # ###,# ### ###,### # # ###,### # ### ###,### ### # #"
        .Split(',')
        .Select(str => (str + "     ").Select(ch => ch == '#').ToArray())
        .ToArray();

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private int[] _flashingLetters;
    private Movement[] _movements;
    private int _curPosition;
    private int _goalPosition;
    private int _buttonRotation;
    private int _squareRotation;
    private Dictionary<int, Movement> _optimalMovements;
    private bool[] _valid;
    private bool _isSolved = false;
    private bool _solveAnimationDone = false;
    private Coroutine _squareFlash;

    string positionToGridColors(int pos)
    {
        return "" +
            _grid[(pos % 16 + 0) % 16 + 16 * ((pos / 16 + 0) % 16)] +
            _grid[(pos % 16 + 1) % 16 + 16 * ((pos / 16 + 0) % 16)] +
            _grid[(pos % 16 + 1) % 16 + 16 * ((pos / 16 + 1) % 16)] +
            _grid[(pos % 16 + 0) % 16 + 16 * ((pos / 16 + 1) % 16)];
    }

    public void Start()
    {
        _moduleId = _moduleIdCounter++;
        _buttonRotation = 3 - (Bomb.GetPortCount() % 4);
        _squareRotation = 3 - (Bomb.GetBatteryCount() % 4);

        tryAgain:
        _curPosition = Rnd.Range(0, _grid.Length);
        _goalPosition = Enumerable.Range(0, _grid.Length).Except(new[] { _curPosition }).PickRandom();
        _valid = Ut.NewArray(256, pos => pos == _goalPosition || (
            _grid[_goalPosition] != _grid[pos] &&
            _grid[(_goalPosition + 1) % 16 + 16 * (_goalPosition / 16)] != _grid[(pos + 1) % 16 + 16 * (pos / 16)] &&
            _grid[_goalPosition % 16 + 16 * ((_goalPosition / 16 + 1) % 16)] != _grid[pos % 16 + 16 * ((pos / 16 + 1) % 16)] &&
            _grid[(_goalPosition + 1) % 16 + 16 * ((_goalPosition / 16 + 1) % 16)] != _grid[(pos + 1) % 16 + 16 * ((pos / 16 + 1) % 16)]));

        _flashingLetters = "ABCDFGHIJKLMNOPQRSUVWXYZ".ToCharArray().Shuffle().Take(4).Select(ch => ch - 'A').ToArray();
        _movements = _flashingLetters.Select(ltr => new Movement(ltr)).ToArray();

        _optimalMovements = new Dictionary<int, Movement>();
        var q = new Queue<int>();
        q.Enqueue(_goalPosition);
        while (q.Count > 0)
        {
            var pos = q.Dequeue();
            foreach (var mv in _movements)
            {
                var n = pos - mv;
                if (n != _goalPosition && _valid[n] && !_optimalMovements.ContainsKey(n))
                {
                    _optimalMovements[n] = mv;
                    q.Enqueue(n);
                }
            }
        }

        if (!_optimalMovements.ContainsKey(_curPosition))
            goto tryAgain;

        var solutionPath = getSolutionPath(_curPosition);
        if (solutionPath.Length < 3 || solutionPath.Length > 5)
            goto tryAgain;

        setPosition(_curPosition, false);
        Debug.LogFormat(@"[Simon Shouts #{0}] Start: {2}{3} = {1} (clockwise from top-left)", _moduleId, positionToGridColors(_curPosition), (char) ('A' + _curPosition % 16), _curPosition / 16 + 1);
        Debug.LogFormat(@"[Simon Shouts #{0}] Goal: {2}{3} = {1} (clockwise from top-left)", _moduleId, positionToGridColors(_goalPosition), (char) ('A' + _goalPosition % 16), _goalPosition / 16 + 1);
        Debug.LogFormat(@"[Simon Shouts #{0}] Movements (clockwise from top): {1}", _moduleId, _flashingLetters.Select((ltr, ix) => string.Format("{0} ({1}, {2})", (char) ('A' + ltr), _movements[ix].XDist, _movements[ix].YDist)).Join(", "));
        Debug.LogFormat(@"[Simon Shouts #{0}] Possible solution: {1}", _moduleId, solutionPath);

        var goalColors = positionToGridColors(_goalPosition);
        for (var i = 0; i < 4; i++)
            ButtonRenderers[(i + _buttonRotation) % 4].sharedMaterial = ButtonMaterials["RGBY".IndexOf(goalColors[i])];

        Module.OnActivate += delegate { StartCoroutine(Flash()); };
        for (var btn = 0; btn < Buttons.Length; btn++)
            Buttons[btn].OnInteract += buttonPress(btn);

        var scalar = transform.lossyScale.x;
        for (var i = 0; i < ButtonLights.Length; i++)
            ButtonLights[i].GetComponent<Light>().range *= scalar;
    }

    private KMSelectable.OnInteractHandler buttonPress(int btn)
    {
        var btnNames = new[] { "top", "right", "bottom", "left" };
        return delegate
        {
            Buttons[btn].AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[btn].transform);
            if (_isSolved)
                return false;

            var newPos = _curPosition + _movements[btn];
            Debug.LogFormat(@"[Simon Shouts #{0}] Pressed {1} button = {5} = ({6}, {7}); new position: {3}{4} = {2}",
                _moduleId, btnNames[btn], positionToGridColors(newPos), (char) ('A' + newPos % 16), newPos / 16 + 1, (char) ('A' + _flashingLetters[btn]), _movements[btn].XDist, _movements[btn].YDist);

            if (newPos == _goalPosition)
            {
                Debug.LogFormat(@"[Simon Shouts #{0}] Module solved.", _moduleId);
                _isSolved = true;
            }
            else if (!_valid[newPos])
            {
                Debug.LogFormat(@"[Simon Shouts #{0}] This position is not valid. Strike.", _moduleId);
                Module.HandleStrike();
                return false;
            }
            else if (!_optimalMovements.ContainsKey(newPos))
            {
                Debug.LogFormat(@"[Simon Shouts #{0}] This position is valid, but there is no path from there to the goal. Strike.", _moduleId);
                Module.HandleStrike();
                return false;
            }
            else
            {
                Debug.LogFormat(@"[Simon Shouts #{0}] Possible solution: {1}", _moduleId, getSolutionPath(newPos));
            }
            setPosition(newPos, _isSolved);
            Audio.PlaySoundAtTransform("ButtonSound" + (btn + 1), transform);
            return false;
        };
    }

    private void setPosition(int newPos, bool solved)
    {
        _curPosition = newPos;
        if (_squareFlash != null)
            StopCoroutine(_squareFlash);
        _squareFlash = StartCoroutine(SetSquares(solved));
    }

    struct AnimationInfo
    {
        public double Time { get; private set; }
        public int Bits { get; private set; }

        public AnimationInfo(double time, int bits) : this()
        {
            Time = time;
            Bits = bits;
        }
    }

    IEnumerator SetSquares(bool solved)
    {
        for (var i = 0; i < 4; i++)
            Squares[(i + _squareRotation) % 4].sharedMaterial = GraySquareMaterial;
        yield return new WaitForSeconds(.08f);
        var colors = positionToGridColors(_curPosition);
        for (var i = 0; i < 4; i++)
            Squares[(i + _squareRotation) % 4].sharedMaterial = SquareMaterials["RGBY".IndexOf(colors[i])];

        if (solved)
        {
            yield return new WaitForSeconds(.5f);
            Audio.PlaySoundAtTransform("SolveSound", transform);
            var animation = Ut.NewArray(
                new AnimationInfo(.703, 0x1),
                new AnimationInfo(.913, 0x0),
                new AnimationInfo(.994, 0xa),
                new AnimationInfo(1.20, 0x0),
                new AnimationInfo(1.31, 0x4),
                new AnimationInfo(1.55, 0x0),
                new AnimationInfo(2.00, 0xf),
                new AnimationInfo(2.28, 0x0));

            var elapsed = 0f;
            var hasSolved = false;
            while (elapsed < animation.Last().Time)
            {
                elapsed += Time.deltaTime;
                var index = animation.LastIndexOf(an => an.Time < elapsed);
                var bits = index == -1 ? (1 << (int) ((elapsed / .08) % 4)) : animation[index].Bits;
                for (var btn = 0; btn < 4; btn++)
                    ButtonLights[btn].SetActive((bits & (1 << btn)) != 0);
                if (elapsed >= 2 && !hasSolved)
                {
                    Module.HandlePass();
                    hasSolved = true;
                    _solveAnimationDone = true;
                }
                yield return null;
            }
        }

        _squareFlash = null;
    }

    string getSolutionPath(int position)
    {
        var solution = new List<char>();
        while (position != _goalPosition)
        {
            var m = _optimalMovements[position];
            solution.Add((char) (m.Letter + 'A'));
            position += m;
        }
        return solution.Join("");
    }

    IEnumerator Flash()
    {
        var t = Rnd.Range(0, 100);
        while (!_isSolved)
        {
            t++;
            for (var i = 0; i < ButtonLights.Length; i++)
                ButtonLights[i].SetActive(_morse[_flashingLetters[i]][t % _morse[_flashingLetters[i]].Length]);
            yield return new WaitForSeconds(.205f);
        }
        for (var i = 0; i < ButtonLights.Length; i++)
            ButtonLights[i].SetActive(false);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} press UDLR [up/down/left/right]";
#pragma warning restore 0414

    IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?:(?:press|submit)\s+)?((?:[udlrtb]\s*)*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        yield return null;
        foreach (var ix in m.Groups[1].Value.Select(ch => "URDLT,B,urdlt,b,".IndexOf(ch) % 4).Where(ix => ix != -1))
        {
            yield return new[] { Buttons[ix] };
            yield return new WaitForSeconds(.2f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        var solution = getSolutionPath(_curPosition);
        foreach (var ltr in solution)
        {
            Buttons[Array.IndexOf(_flashingLetters, ltr - 'A')].OnInteract();
            yield return new WaitForSeconds(.2f);
        }
        while (!_solveAnimationDone)
            yield return null;
    }
}
