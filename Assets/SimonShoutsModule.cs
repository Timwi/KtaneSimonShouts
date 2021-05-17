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
    public KMRuleSeedable RuleSeedable;

    public MeshRenderer[] Squares;
    public Material[] SquareMaterials;
    public Material GraySquareMaterial;
    public GameObject[] ButtonLights;
    public KMSelectable[] Buttons;
    public MeshRenderer[] ButtonRenderers;
    public Material[] ButtonMaterials;

    private static readonly bool[][] _morse = "# ###,### # # #,### # ### #,### # #,#,# # ### #,### ### #,# # # #,# #,# ### ### ###,### # ###,# ### # #,### ###,### #,### ### ###,# ### ### #,### ### # ###,# ### #,# # #,###,# # ###,# # # ###,# ### ###,### # # ###,### # ### ###,### ### # #"
        .Split(',')
        .Select(str => (str + "     ").Select(ch => ch == '#').ToArray())
        .ToArray();

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private int[] _diagramBPositions;
    private Movement[] _movements;
    private int _curPosition;
    private int _goalPosition;
    private Dictionary<int, Movement> _optimalMovements;
    private bool[] _valid;
    private bool _isSolved = false;
    private bool _solveAnimationDone = false;
    private Coroutine _squareFlash;

    // Rule-related (varies with rule seed)
    private int _buttonRotation;
    private int _squareRotation;
    private string _grid;
    private string _diagramB;

    private static readonly string[] _debruijnSequences = new[] { "0011031221302332", "0011031223302132", "0011031230213322", "0011031233213022", "0011032130223312", "0011032133123022", "0011032230213312", "0011033021322312", "0011033023122132", "0011033221302312", "0011203213022331", "0011203223021331", "0011203302132231", "0011203322130231", "0011221302332031", "0011221320330231", "0011223302132031", "0011230213322031", "0011230220321331", "0011233213022031", "0011302103122332", "0011302103123322", "0011302103223312", "0011302103322312", "0011302233210312", "0011302312210332", "0011302332210312", "0011322103302312", "0011322330210312", "0011331230210322", "0011332103123022", "0011332230210312", "0012031130223321", "0012031130233221", "0012031132233021", "0012031133223021", "0012032113302231", "0012032130223311", "0012032230211331", "0012032230213311", "0012032231133021", "0012032233113021", "0012033021132231", "0012033021322311", "0012033022311321", "0012033023113221", "0012033113223021", "0012033221302311", "0012033223113021", "0012203113023321", "0012203302311321", "0012203321130231", "0012210311302332", "0012210330231132", "0012211302332031", "0012211320330231", "0012213023110332", "0012213023320311", "0012213203302311", "0012231103302132", "0012231130210332", "0012231132033021", "0012233021031132", "0012233021320311", "0012233203113021", "0012302103113322", "0012302103311322", "0012302113220331", "0012302113310322", "0012302133110322", "0012302133220311", "0012302203113321", "0012302203211331", "0012302203213311", "0012302203311321", "0012310321133022", "0012310330211322", "0012311302203321", "0012311320330221", "0012311321033022", "0012311322033021", "0012311330210322", "0012311330220321", "0012330220311321", "0012330221132031", "0012331103213022", "0012331130210322", "0012331130220321", "0012332031130221", "0012332103113022", "0012332113022031", "0012332130220311", "0012332203113021", "0013210330223112", "0013210331123022", "0013211231033022", "0013223021033112", "0013223103302112", "0013302103223112", "0013302112310322", "0013302231032112", "0013310321123022", "0013310322302112", "0013311230210322", "0110021320312233", "0110021322312033", "0110021331203223", "0110021332203123", "0110022031233213", "0110022032133123", "0110022331203213", "0110023120332213", "0110023122132033", "0110023320312213", "0110213003122332", "0110213003123322", "0110213003223312", "0110213003322312", "0110213200331223", "0110213203122330", "0110213223003312", "0110213223120033", "0110213223120330", "0110213223300312", "0110213300322312", "0110213312003223", "0110213312032230", "0110213312230032", "0110213312300322", "0110213322031230", "0110213322300312", "0110220031233213", "0110220032133123", "0110220312332130", "0110220321331230", "0110221300312332", "0110221331230032", "0110221332003123", "0110223003213312", "0110223312032130", "0110223321300312", "0110230031221332", "0110230032133122", "0110230032213312", "0110230033122132", "0110231200322133", "0110231203322130", "0110231221300332", "0110231221320033", "0110231221320330", "0110231221330032", "0110233003122132", "0110233122003213", "0110233122130032", "0110233200312213", "0110233203122130", "0110233213003122", "0110233221300312", "0112002132231033", "0112002133103223", "0112002310322133", "0112003221331023", "0112003223102133", "0112003310213223", "0112031021322330", "0112031021332230", "0112031022332130", "0112031023322130", "0112032133102230", "0112032233102130", "0112033223102130", "0112213003102332", "0112213023100332", "0112213200231033", "0112213200331023", "0112213203102330", "0112213300231032", "0112213310230032", "0112213310320023", "0112230021331032", "0112230033102132", "0112231003302132", "0112231021300332", "0112231021320033", "0112231021320330", "0112231021330032", "0112231032002133", "0112231033002132", "0112233003102132", "0112233203102130", "0112300310213322", "0112300321331022", "0112302133100322", "0112331003213022", "0112331021300322", "0112331022032130", "0112332130031022", "0112332203102130", "0120021031132233", "0120021031133223", "0120021032231133", "0120021032233113", "0120021033113223", "0120021033223113", "0120021132231033", "0120021133103223", "0120021322311033", "0120021331103223", "0120022310321133", "0120022311321033", "0120022331103213", "0120022332103113", "0120023110332213", "0120023113221033", "0120023322103113", "0120311002132233", "0120311002133223", "0120311002233213", "0120311002332213", "0120311322100233", "0120311332100223", "0120321002231133", "0120321002233113", "0120321133100223", "0120321331100223", "0120322310021133", "0120322331100213", "0120331002113223", "0120331132100223", "0120332210023113", "0120332231100213", "0122002310321133", "0122002311321033", "0122002331103213", "0122002332103113", "0122003110233213", "0122003113321023", "0122003210231133", "0122003210233113", "0122003211331023", "0122003213311023", "0122003311321023", "0122031023321130", "0122031132102330", "0122033210231130", "0122100231132033", "0122100233203113", "0122100311302332", "0122100330231132", "0122102300311332", "0122102300331132", "0122102311300332", "0122102311320033", "0122102311320330", "0122102311330032", "0122102330031132", "0122102331130032", "0122102332003113", "0122102332031130", "0122103113002332", "0122103113200233", "0122103113320023", "0122103200231133", "0122103200233113", "0122103300231132", "0122103311320023", "0122103320023113", "0122113003102332", "0122113023100332", "0122113200231033", "0122113200331023", "0122113203102330", "0122113300231032", "0122113310230032", "0122113310320023", "0122130023311032", "0122130031102332", "0122132002311033", "0122132031100233", "0122133110230032", "0122133110320023", "0122133200311023", "0122311002132033", "0122311033200213", "0122311320021033", "0122332002103113", "0122332031100213", "0123002210311332", "0123002210331132", "0123002211331032", "0123002213311032", "0123003110221332", "0123003113321022", "0123003211331022", "0123003213311022", "0123003310221132", "0123003311321022", "0123100211322033", "0123100220321133", "0123100330221132", "0123100332113022", "0123102200321133", "0123102203321130", "0123102211300332", "0123102211320033", "0123102211320330", "0123102211330032", "0123103200221133", "0123103211330022", "0123103220021133", "0123103300221132", "0123113003321022", "0123113022100332", "0123113200221033", "0123113210022033", "0123113210033022", "0123113210220033", "0123113210220330", "0123113210330022", "0123113220021033", "0123113300221032", "0123113300321022", "0123300310221132", "0123300311321022", "0123302210031132", "0123311002203213", "0123311022003213", "0123311022130032", "0123311032002213", "0123311032130022", "0123311032200213", "0123311300221032", "0123311300321022", "0123320022103113", "0123320031102213", "0123320310221130", "0123321002203113", "0123321003113022", "0123321022003113", "0123321022031130", "0123321031130022", "0123321130031022", "0123321300311022", "0123322002103113", "0123322031100213" };

    string positionToGridColors(int pos)
    {
        return "" +
            _grid[(pos % 16 + 0) % 16 + 16 * ((pos / 16 + 0) % 16)] +
            _grid[(pos % 16 + 1) % 16 + 16 * ((pos / 16 + 0) % 16)] +
            _grid[(pos % 16 + 1) % 16 + 16 * ((pos / 16 + 1) % 16)] +
            _grid[(pos % 16 + 0) % 16 + 16 * ((pos / 16 + 1) % 16)];
    }

    private void setTorus(int seqIx, int rotIx, char[] coloring, int cycleX, int cycleY, bool transpose, bool mirrorX, bool mirrorY)
    {
        var seq = _debruijnSequences[seqIx].Substring(rotIx) + _debruijnSequences[seqIx].Substring(0, rotIx);
        _grid = Enumerable.Range(0, 256).Select(ix =>
        {
            var i = (ix + cycleX) % 16;
            var j = (ix / 16 + cycleY) % 16;
            if (mirrorX)
                i = 15 - i;
            if (mirrorY)
                j = 15 - j;
            return coloring[seq[((i + j) % 2 == 0) ^ transpose ? i : j] - '0'];
        }).Join("");
    }

    public void Start()
    {
        _moduleId = _moduleIdCounter++;

        // RULE SEED
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat(@"[Simon Shouts #{0}] Using rule seed: {1}.", _moduleId, rnd.Seed);
        if (rnd.Seed == 1)
        {
            setTorus(0, 0, new[] { 'R', 'G', 'Y', 'B' }, 0, 0, false, false, false);
            _buttonRotation = 3 - (Bomb.GetPortCount() % 4);
            _squareRotation = 3 - (Bomb.GetBatteryCount() % 4);
            _diagramB = "ABCDFGHIJKLMNOPQRSUVWXYZ";
        }
        else
        {
            var coloring = new[] { 'R', 'G', 'B', 'Y' };
            rnd.ShuffleFisherYates(coloring);

            setTorus(
                rnd.Next(0, _debruijnSequences.Length),
                rnd.Next(0, 16),
                coloring,
                rnd.Next(0, 16),
                rnd.Next(0, 16),
                rnd.Next(0, 2) != 0,
                rnd.Next(0, 2) != 0,
                rnd.Next(0, 2) != 0);

            var edgeworkRules = Ut.NewArray<Func<KMBombInfo, int>>(
                b => b.GetPortCount(), // ports
                b => b.CountUniquePorts(), // port types
                b => b.GetPortPlateCount(), // port plates
                b => b.GetPortPlates().Count(p => p.Length > 0), // non-empty port plates
                b => b.GetBatteryCount(), // batteries
                b => b.GetBatteryCount(Battery.D), // D batteries
                b => b.GetIndicators().Count(), // indicators
                b => b.GetOnIndicators().Count(), // lit indicators
                b => b.GetOffIndicators().Count(), // unlit indicators
                b => b.GetSerialNumberNumbers().Count(), // digits in the serial number
                b => b.GetSerialNumberNumbers().Count(d => d % 2 == 0), // even digits in the serial number
                b => b.GetSerialNumberNumbers().Count(d => d % 2 == 1), // odd digits in the serial number
                b => b.GetSerialNumberLetters().Count("AEIOU".Contains), // vowels in the serial number
                b => b.GetSerialNumberLetters().Count(ch => !"AEIOU".Contains(ch)), // consonants in the serial number
                b => b.GetModuleIDs().Count, // modules
                b => b.GetSolvableModuleIDs().Count, // regular modules
                b => b.GetModuleIDs().Count() - Bomb.GetSolvableModuleIDs().Count // needy modules
            );
            rnd.ShuffleFisherYates(edgeworkRules);

            _buttonRotation = 3 - (edgeworkRules[0](Bomb) % 4);
            _squareRotation = 3 - (edgeworkRules[1](Bomb) % 4);

            var alphabet = new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            rnd.ShuffleFisherYates(alphabet);
            _diagramB = new string(alphabet).Substring(0, 24);
        }

        tryAgain:
        _curPosition = Rnd.Range(0, _grid.Length);
        _goalPosition = Enumerable.Range(0, _grid.Length).Except(new[] { _curPosition }).PickRandom();
        _valid = Ut.NewArray(256, pos => pos == _goalPosition || (
            _grid[_goalPosition] != _grid[pos] &&
            _grid[(_goalPosition + 1) % 16 + 16 * (_goalPosition / 16)] != _grid[(pos + 1) % 16 + 16 * (pos / 16)] &&
            _grid[_goalPosition % 16 + 16 * ((_goalPosition / 16 + 1) % 16)] != _grid[pos % 16 + 16 * ((pos / 16 + 1) % 16)] &&
            _grid[(_goalPosition + 1) % 16 + 16 * ((_goalPosition / 16 + 1) % 16)] != _grid[(pos + 1) % 16 + 16 * ((pos / 16 + 1) % 16)]));

        _diagramBPositions = Enumerable.Range(0, 24).ToList().Shuffle().Take(4).ToArray();
        _movements = _diagramBPositions.Select(dBpos => new Movement(dBpos)).ToArray();

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
        Debug.LogFormat(@"[Simon Shouts #{0}] Movements (clockwise from top): {1}", _moduleId, _diagramBPositions.Select((dBpos, ix) => string.Format("{0} ({1}, {2})", _diagramB[dBpos], _movements[ix].XDist, _movements[ix].YDist)).Join(", "));
        Debug.LogFormat(@"[Simon Shouts #{0}] Possible solution: {1}", _moduleId, solutionPath.Select(dBpos => _diagramB[dBpos]).Join(""));

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
                _moduleId, btnNames[btn], positionToGridColors(newPos), (char) ('A' + newPos % 16), newPos / 16 + 1, _diagramB[_diagramBPositions[btn]], _movements[btn].XDist, _movements[btn].YDist);

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
                Debug.LogFormat(@"[Simon Shouts #{0}] Possible solution: {1}", _moduleId, getSolutionPath(newPos).Select(dBpos => _diagramB[dBpos]).Join(""));
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

    // Returns diagram B positions
    int[] getSolutionPath(int position)
    {
        var solution = new List<int>();
        while (position != _goalPosition)
        {
            var m = _optimalMovements[position];
            solution.Add(m.DiagramBPosition);
            position += m;
        }
        return solution.ToArray();
    }

    IEnumerator Flash()
    {
        var t = Rnd.Range(0, 100);
        while (!_isSolved)
        {
            t++;
            for (var i = 0; i < ButtonLights.Length; i++)
            {
                var morse = _morse[_diagramB[_diagramBPositions[i]] - 'A'];
                ButtonLights[i].SetActive(morse[t % morse.Length]);
            }
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
        foreach (var dbPos in solution)
        {
            Buttons[Array.IndexOf(_diagramBPositions, dbPos)].OnInteract();
            yield return new WaitForSeconds(.2f);
        }
        while (!_solveAnimationDone)
            yield return null;
    }
}
