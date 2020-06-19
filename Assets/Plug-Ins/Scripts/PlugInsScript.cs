using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Xsl;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class PlugInsScript : MonoBehaviour
{

    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public KMSelectable Initiate;
    public TextMesh[] Labels;
    public Material Black;
    public Material[] Receptacles;
    public MeshRenderer[] Duplex;
    public KMRuleSeedable KMRuleSeedable;

    static int moduleIdCounter = 1;
    int moduleId;
    bool moduleSolved = false;
    bool resetActive = false;
    bool initialReset = true;
    int correctBtn;
    int currentStage = 0;
    private bool inputAnim;
    Coroutine timer;
    bool tpPress = false;
    bool timerStrike = false;

    List<string> devices = new List<string>() { "Bedside lamp", "Waffle iron", "Washing machine", "Window fan", "Blender", "Vacuum cleaner", "Desk fan", "DVD player", "Franks 2000 inch TV", "Hairdryer", "Iron", "Laptop charger", "Microwave", "Sewing machine", "Solar water heater", "Telephone", "Toaster", "Nightlight", "Paper shredder", "Printer", "Radiator", "Refrigerator", "Air conditioner", "Stereo system", "VHC recorder", "Alarm clock", "Answering machine", "Barbecue grill", "Blow dryer", "Burglar alarm", "Calculator", "Camera", "Can opener", "CD player", "Ceiling fan", "Cell phone", "Clock", "Clothes dryer", "Coffee grinder", "Coffee maker", "Computer", "Convection oven", "Copier", "Crock pot", "Curling iron", "Dishwasher", "Doorbell", "Edge trimmer", "Electric drill", "Electric guitar", "Electric razor", "Electric toothbrush", "Espresso maker", "Fax machine", "Food processor", "Freezer", "Garage door", "Headphones", "Hot plate", "Humidifier", "Ice cream maker", "Lawn mower", "Leaf blower", "Oven", "Mixer", "MP3 player", "Pressure cooker", "Radio", "Record player", "Rotisserie", "Scanner", "Smoke detector", "Stove", "Trash compactor", "Vaporizer", };

    void Start()
    {
        moduleId = moduleIdCounter++;

        for (var i = 0; i < Buttons.Length; i++)
            Buttons[i].OnInteract += ButtonPressed(i);

        Initiate.OnInteract += delegate
        {
            if (moduleSolved || resetActive)
                return false;
            StartCoroutine(Input());
            return false;
        };
        var rnd = KMRuleSeedable.GetRNG();
        if (rnd.Seed != 1)
        {
            rnd.ShuffleFisherYates(Receptacles);
            rnd.ShuffleFisherYates(devices);
        }
        StartCoroutine(Reset());
    }

    KMSelectable.OnInteractHandler ButtonPressed(int btn)
    {
        return delegate ()
        {
            if (moduleSolved || resetActive)
                return false;
            if (btn == correctBtn)
            {
                currentStage++;
                if (currentStage < 3)
                {
                    initialReset = true;
                    StartCoroutine(Reset());
                }
                else
                    StartCoroutine(Solved());
            }
            else
            {
                timerStrike = false;
                StartCoroutine(Reset());
            }
            if (timer != null)
                StopCoroutine(timer);
            timer = null;
            return false;
        };

    }

    IEnumerator Input()
    {
        inputAnim = true;
        for (var i = 0; i < Duplex.Length; i++)
            Duplex[i].material = Black;

        var duration = .2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            Initiate.transform.localPosition = Vector3.Lerp(Initiate.transform.localPosition, new Vector3(Initiate.transform.localPosition.x, 0.031f, Initiate.transform.localPosition.z), elapsed / duration);
            for (var i = 0; i < Buttons.Length; i++)
                Buttons[i].transform.localPosition = Vector3.Lerp(Buttons[i].transform.localPosition, new Vector3(Buttons[i].transform.localPosition.x, 0.031f, Buttons[i].transform.localPosition.z), elapsed / duration);
        }

        duration = .2f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            Initiate.transform.localPosition = Vector3.Lerp(Initiate.transform.localPosition, new Vector3(Initiate.transform.localPosition.x, 0.025f, Initiate.transform.localPosition.z), elapsed / duration);
            for (var i = 0; i < Buttons.Length; i++)
                Buttons[i].transform.localPosition = Vector3.Lerp(Buttons[i].transform.localPosition, new Vector3(Buttons[i].transform.localPosition.x, 0.03f, Buttons[i].transform.localPosition.z), elapsed / duration);
        }
        for (var i = 0; i < Buttons.Length; i++)
            GetComponent<KMSelectable>().Children[i] = Buttons[i];
        GetComponent<KMSelectable>().Children[5] = null;
        GetComponent<KMSelectable>().UpdateChildren();
        inputAnim = false;
        timer = StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        timerStrike = true;
        var elapsed = 0f;
        var duration = tpPress ? 24f : 3f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        StartCoroutine(Reset());
        tpPress = false;
    }

    void Generate()
    {
        var ixs = Enumerable.Range(0, Receptacles.Length).ToList().Shuffle();

        var usedDuplex = 0;
        var remainingDuplex = 0;
        if (Rnd.Range(0, 2) == 0)
            remainingDuplex = 1;
        else
            usedDuplex = 1;

        Duplex[usedDuplex].material = Receptacles[ixs[0]];
        Duplex[remainingDuplex].material = Receptacles[ixs[1]];

        correctBtn = Rnd.Range(0, Labels.Length);

        Labels[correctBtn].text = devices[ixs[0]];

        var remainingLabels = Enumerable.Range(0, Labels.Length).Where(rL => rL != correctBtn).ToList();
        for (var i = 0; i < remainingLabels.Count; i++)
            Labels[remainingLabels[i]].text = devices[ixs[i + 2]];

        Debug.LogFormat(@"[Plug-Ins #{0}] Selected receptacles - {1} and {2}", moduleId, usedDuplex == 0 ? devices[ixs[0]] : devices[ixs[1]], usedDuplex == 0 ? devices[ixs[1]] : devices[ixs[0]]);
        Debug.LogFormat(@"[Plug-Ins #{0}] Button labels in reading order - {1}", moduleId, Labels.Select(l => l.text).ToList().Join(", "));
        Debug.LogFormat(@"[Plug-Ins #{0}] Correct button to press reading order - {1} ({2})", moduleId, correctBtn + 1, Labels[correctBtn].text);
    }


    IEnumerator Reset()
    {
        for (var i = 0; i < Duplex.Length; i++)
            Duplex[i].material = Black;
        if (initialReset)
            initialReset = false;
        else
        {
            Debug.LogFormat(@"[Plug-Ins #{0}] Strike! - {1}", moduleId, timerStrike ? "Time-out!" : "Incorrect button!");
            BombModule.HandleStrike();
        }

        resetActive = true;

        var duration = .2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            Initiate.transform.localPosition = Vector3.Lerp(Initiate.transform.localPosition, new Vector3(Initiate.transform.localPosition.x, 0.031f, Initiate.transform.localPosition.z), elapsed / duration);
            for (var i = 0; i < Buttons.Length; i++)
                Buttons[i].transform.localPosition = Vector3.Lerp(Buttons[i].transform.localPosition, new Vector3(Buttons[i].transform.localPosition.x, 0.031f, Buttons[i].transform.localPosition.z), elapsed / duration);
        }

        duration = .2f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            Initiate.transform.localPosition = Vector3.Lerp(Initiate.transform.localPosition, new Vector3(Initiate.transform.localPosition.x, 0.03f, Initiate.transform.localPosition.z), elapsed / duration);
            for (var i = 0; i < Buttons.Length; i++)
                Buttons[i].transform.localPosition = Vector3.Lerp(Buttons[i].transform.localPosition, new Vector3(Buttons[i].transform.localPosition.x, 0.025f, Buttons[i].transform.localPosition.z), elapsed / duration);
        }

        for (var i = 0; i < Buttons.Length; i++)
            GetComponent<KMSelectable>().Children[i] = null;
        GetComponent<KMSelectable>().Children[5] = Initiate;
        GetComponent<KMSelectable>().UpdateChildren();
        resetActive = false;
        Generate();
    }

    IEnumerator Solved()
    {
        moduleSolved = true;
        for (var i = 0; i < Duplex.Length; i++)
            Duplex[i].material = Black;

        for (var i = 0; i < Buttons.Length; i++)
            GetComponent<KMSelectable>().Children[i] = null;
        GetComponent<KMSelectable>().Children[5] = null;
        GetComponent<KMSelectable>().UpdateChildren();

        var duration = .2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            Initiate.transform.localPosition = Vector3.Lerp(Initiate.transform.localPosition, new Vector3(Initiate.transform.localPosition.x, 0.031f, Initiate.transform.localPosition.z), elapsed / duration);
            for (var i = 0; i < Buttons.Length; i++)
                Buttons[i].transform.localPosition = Vector3.Lerp(Buttons[i].transform.localPosition, new Vector3(Buttons[i].transform.localPosition.x, 0.031f, Buttons[i].transform.localPosition.z), elapsed / duration);
        }

        duration = .2f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            Initiate.transform.localPosition = Vector3.Lerp(Initiate.transform.localPosition, new Vector3(Initiate.transform.localPosition.x, 0.025f, Initiate.transform.localPosition.z), elapsed / duration);
            for (var i = 0; i < Buttons.Length; i++)
                Buttons[i].transform.localPosition = Vector3.Lerp(Buttons[i].transform.localPosition, new Vector3(Buttons[i].transform.localPosition.x, 0.025f, Buttons[i].transform.localPosition.z), elapsed / duration);
        }
        BombModule.HandlePass();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} start,green,go,round,activate,plugin [Press the green button (time is increased to 15 seconds on TP)] | !{0} 1,2,3,4,5 [Press the button in that position (1=tl, 2=tr, 3=mid, 4=bl, 5=br)]";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if (moduleSolved)
        {
            yield return "sendtochaterror The module is already solved.";
            yield break;
        }
        else if (Regex.IsMatch(command, @"^\s*start|green|go|round|activate|plugin\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            tpPress = true;
            Initiate.OnInteract();
            yield break;
        }
        else if ((m = Regex.Match(command, @"^\s*([12345])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            var btn = int.Parse(m.Groups[1].Value);
            Buttons[btn - 1].OnInteract();
            yield return "solve";
            yield break;
        }
        else
        {
            yield return "sendtochaterror Invalid Command";
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat(@"[Plug-Ins #{0}] Module was force solved by TP", moduleId);

        if (timer != null)
        {
            Buttons[correctBtn].OnInteract();
            yield return true;
            yield return new WaitUntil(() => !resetActive);
        }

        while (!moduleSolved)
        {
            if (moduleSolved)
                break;
            Initiate.OnInteract();
            yield return true;
            yield return new WaitUntil(() => !inputAnim);
            Buttons[correctBtn].OnInteract();
            yield return true;
            yield return new WaitUntil(() => !resetActive);
        }
    }
}
