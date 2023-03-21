using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class DialTrialScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public GameObject Base;
    public Mesh[] Symbols;
    public SpriteRenderer Steam;

    private Coroutine ButtonPressAnim;
    private Vector3 SubmitButtonPos;
    private List<GameObject> BaseSymbolParents = new List<GameObject>();
    private List<int>[] Numbers = new List<int>[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
    private List<int> Positions = new List<int>() { 0, 0 };
    private static readonly float[][][] SymbolPlaces = new float[][][] {
        new float[][] { new float[] { 0, 0.009f }, new float[] { 0.0004710238f, 0.008987674f }, new float[] { 0.000940756f, 0.008950702f }, new float[] { 0.001407912f, 0.008889215f }, new float[] { 0.001871206f, 0.008803334f } },
        new float[][] { new float[] { 0, 0.0088f }, new float[] { 0.0005372277f, 0.008783585f }, new float[] { 0.001072451f, 0.008734406f }, new float[] { 0.001603671f, 0.008652643f }, new float[] { 0.002128912f, 0.008538603f } },
        new float[][] { new float[] { 0, 0.0085f }, new float[] { 0.0006669024f, 0.008473802f }, new float[] { 0.001329693f, 0.008395356f }, new float[] { 0.001984289f, 0.008265149f }, new float[] { 0.002626648f, 0.008083986f } },
        new float[][] { new float[] { 0, 0.008f }, new float[] { 0.0009056271f, 0.007948583f }, new float[] { 0.001799611f, 0.007794968f }, new float[] { 0.002670459f, 0.007541139f }, new float[] { 0.003506975f, 0.007190359f } }
    };
    private bool[] Pressing = new bool[2];
    private bool Solved;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        do
            for (int i = 0; i < 2; i++)
                Positions[i] = Rnd.Range(0, 4);
        while (Positions.Sum() == 0);
        BaseSymbolParents.Add(Base.transform.Find("Symbols Parent").gameObject);
        for (int i = 0; i < Buttons.Length; i++)
            BaseSymbolParents.Add(Buttons[i].transform.Find("Symbols Parent").gameObject);
        for (int i = 0; i < Buttons.Length; i++)
        {
            int x = i;
            Buttons[x].OnInteract += delegate { ButtonPress(x); return false; };
        }
        Module.OnActivate += delegate
        {
            for (int i = 0; i < 4; i++)
                foreach (MeshRenderer symbol in BaseSymbolParents[i].GetComponentsInChildren<MeshRenderer>())
                    if (symbol.GetComponent<MeshFilter>().sharedMesh == Symbols[1])
                        symbol.material.color = new Color32(31, 123, 201, 255);
        };
        Calculate();
        SubmitButtonPos = Buttons[2].transform.localPosition;
        for (int i = 0; i < BaseSymbolParents.Count; i++)
            foreach (Transform symbol in BaseSymbolParents[i].transform.GetComponentsInChildren<Transform>())
                if (new[] { "Circle", "Diamond" }.Contains(symbol.name))
                    symbol.gameObject.SetActive(false);
    }

    void Calculate()
    {
        restart:
        int[] order = new int[] { 0, 1, 2, 3 }.Shuffle();
        int sum = Rnd.Range(15, 19);
        for (int i = 0; i < 4; i++)
        {
            retry:
            List<int> randNums = new List<int>();
            for (int j = 0; j < 3; j++)
            {
                int random = 0;
                int tries = 0;
                do
                {
                    random = (Rnd.Range(0, 14) % 9) + 1;
                    tries++;
                    if (tries > 1000)
                        goto restart;
                }
                while (randNums.DefaultIfEmpty(0).Contains(random) || randNums.DefaultIfEmpty(0).Sum() + random >= sum - 2 + i || (randNums.DefaultIfEmpty(0).Sum() + random < sum - 9 && j == 2));
                randNums.Add(random);
            }
            if ((randNums[0] == randNums[1] && randNums[1] == randNums[2] && randNums[2] == randNums[3]) || randNums.Sum() == sum)
                goto retry;
            Numbers[order[i]] = randNums.ToList();
        }
        for (int i = 0; i < 4; i++)
            Numbers[i].Add(sum - Numbers[i][0] - Numbers[i][1] - Numbers[i][2]);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                if (Numbers[0][0] + Numbers[0][3] + Numbers[i][1] + Numbers[j][2] == sum && Numbers[1][0] + Numbers[1][3] + Numbers[(i + 1) % 4][1] + Numbers[(j + 1) % 4][2] == sum && Numbers[2][0] + Numbers[2][3] + Numbers[(i + 2) % 4][1] + Numbers[(j + 1) % 4][2] == sum && Numbers[3][0] + Numbers[3][3] + Numbers[(i + 3) % 4][1] + Numbers[(j + 3) % 4][2] == sum && (i != 0 || j != 0))
                    goto restart;
        for (int i = 0; i < 4; i++)
            if (Numbers[0][i] == Numbers[2][i] && Numbers[1][i] == Numbers[3][i])
                goto restart;
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                InstantiateSymbols(i, j);
        List<int>[] temp = new List<int>[] { Numbers.Select(x => x[0]).ToList(), new List<int>(), new List<int>(), Numbers.Select(x => x[3]).ToList() };
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 4; j++)
                temp[i + 1].Add(Numbers[(j - Positions[i] + 4) % 4][i + 1]);
        for (int i = 0; i < 4; i++)
            Debug.LogFormat("[Dial Trial #{0}] Quadrant {1}{3}: {2}.", _moduleID, i + 1, temp.Select(x => x[i]).Join(", "), i == 0 ? "'s numbers are (outer ring to inner ring)" : "");
        Debug.LogFormat("[Dial Trial #{0}] The required total of each quadrant is {1}.", _moduleID, sum);
        for (int i = 0; i < 4; i++)
            Debug.LogFormat("[Dial Trial #{0}] {3}or quadrant {1}{4}: {2}.", _moduleID, i + 1, Numbers[i].Join(", "), i == 0 ? "The solution numbers f" : "F", i == 0 ? " (outer ring to inner ring)" : "");
    }

    void InstantiateSymbols(int pos, int dir)
    {
        List<int> symbols = new List<int>();
        if (pos > 0 && pos < 3)
        {
            if (Numbers[(dir + 4 - Positions[pos - 1]) % 4][pos] > 4)
                symbols.Add(5);
            for (int i = 0; i < Numbers[(dir + 4 - Positions[pos - 1]) % 4][pos] % 5; i++)
                symbols.Add(1);
        }
        else
        {
            if (Numbers[dir][pos] > 4)
                symbols.Add(5);
            for (int i = 0; i < Numbers[dir][pos] % 5; i++)
                symbols.Add(1);
        }
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        for (int i = 0; i < symbols.Count; i++)
        {
            meshFilters.Add(Instantiate(BaseSymbolParents[pos].GetComponentsInChildren<MeshFilter>().Where(x => x.name == new[] { "Circle", "Diamond" }[symbols[i] / 5]).First(), BaseSymbolParents[pos].transform));
            meshFilters[i].transform.localScale = BaseSymbolParents[pos].transform.GetChild(symbols[i] / 5).transform.localScale;
            meshFilters[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0, 1);
            meshFilters[i].mesh = Symbols[symbols[i] / 5];
        }
        switch (symbols.Count)
        {
            case 1:
                switch (dir)
                {
                    case 0:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][0][0], SymbolPlaces[pos][0][1], meshFilters[0].transform.localPosition.z);
                        break;
                    case 1:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][0][1], SymbolPlaces[pos][0][0], meshFilters[0].transform.localPosition.z);
                        break;
                    case 2:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][0][0], SymbolPlaces[pos][0][1] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                    default:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][0][1] * -1, SymbolPlaces[pos][0][0] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                }
                break;
            case 2:
                switch (dir)
                {
                    case 0:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][1][0] * -1, SymbolPlaces[pos][1][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][1][0], SymbolPlaces[pos][1][1], meshFilters[0].transform.localPosition.z);
                        break;
                    case 1:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][1][1], SymbolPlaces[pos][1][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][1][1], SymbolPlaces[pos][1][0] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                    case 2:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][1][0], SymbolPlaces[pos][1][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][1][0] * -1, SymbolPlaces[pos][1][1] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                    default:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][1][1] * -1, SymbolPlaces[pos][1][0] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][1][1] * -1, SymbolPlaces[pos][1][0], meshFilters[0].transform.localPosition.z);
                        break;
                }
                break;
            case 3:
                switch (dir)
                {
                    case 0:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][2][0] * -1, SymbolPlaces[pos][2][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][0][0], SymbolPlaces[pos][0][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][2][0], SymbolPlaces[pos][2][1], meshFilters[0].transform.localPosition.z);
                        break;
                    case 1:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][2][1], SymbolPlaces[pos][2][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][0][1], SymbolPlaces[pos][0][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][2][1], SymbolPlaces[pos][2][0] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                    case 2:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][2][0], SymbolPlaces[pos][2][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][0][0], SymbolPlaces[pos][0][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][2][0] * -1, SymbolPlaces[pos][2][1] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                    default:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][2][1] * -1, SymbolPlaces[pos][2][0] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][0][1] * -1, SymbolPlaces[pos][0][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][2][1] * -1, SymbolPlaces[pos][2][0], meshFilters[0].transform.localPosition.z);
                        break;
                }
                break;
            case 4:
                switch (dir)
                {
                    case 0:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][3][0] * -1, SymbolPlaces[pos][3][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][1][0] * -1, SymbolPlaces[pos][1][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][1][0], SymbolPlaces[pos][1][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[3].transform.localPosition = new Vector3(SymbolPlaces[pos][3][0], SymbolPlaces[pos][3][1], meshFilters[0].transform.localPosition.z);
                        break;
                    case 1:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][3][1], SymbolPlaces[pos][3][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][1][1], SymbolPlaces[pos][1][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][1][1], SymbolPlaces[pos][1][0] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[3].transform.localPosition = new Vector3(SymbolPlaces[pos][3][1], SymbolPlaces[pos][3][0] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                    case 2:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][3][0], SymbolPlaces[pos][3][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][1][0], SymbolPlaces[pos][1][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][1][0] * -1, SymbolPlaces[pos][1][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[3].transform.localPosition = new Vector3(SymbolPlaces[pos][3][0] * -1, SymbolPlaces[pos][3][1] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                    default:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][3][1] * -1, SymbolPlaces[pos][3][0] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][1][1] * -1, SymbolPlaces[pos][1][0] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][1][1] * -1, SymbolPlaces[pos][1][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[3].transform.localPosition = new Vector3(SymbolPlaces[pos][3][1] * -1, SymbolPlaces[pos][3][0], meshFilters[0].transform.localPosition.z);
                        break;
                }
                break;
            default:
                switch (dir)
                {
                    case 0:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][4][0] * -1, SymbolPlaces[pos][4][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][2][0] * -1, SymbolPlaces[pos][2][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][0][0], SymbolPlaces[pos][0][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[3].transform.localPosition = new Vector3(SymbolPlaces[pos][2][0], SymbolPlaces[pos][2][1], meshFilters[0].transform.localPosition.z);
                        meshFilters[4].transform.localPosition = new Vector3(SymbolPlaces[pos][4][0], SymbolPlaces[pos][4][1], meshFilters[0].transform.localPosition.z);
                        break;
                    case 1:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][4][1], SymbolPlaces[pos][4][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][2][1], SymbolPlaces[pos][2][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][0][1], SymbolPlaces[pos][0][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[3].transform.localPosition = new Vector3(SymbolPlaces[pos][2][1], SymbolPlaces[pos][2][0] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[4].transform.localPosition = new Vector3(SymbolPlaces[pos][4][1], SymbolPlaces[pos][4][0] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                    case 2:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][4][0], SymbolPlaces[pos][4][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][2][0], SymbolPlaces[pos][2][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][0][0], SymbolPlaces[pos][0][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[3].transform.localPosition = new Vector3(SymbolPlaces[pos][2][0] * -1, SymbolPlaces[pos][2][1] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[4].transform.localPosition = new Vector3(SymbolPlaces[pos][4][0] * -1, SymbolPlaces[pos][4][1] * -1, meshFilters[0].transform.localPosition.z);
                        break;
                    default:
                        meshFilters[0].transform.localPosition = new Vector3(SymbolPlaces[pos][4][1] * -1, SymbolPlaces[pos][4][0] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[1].transform.localPosition = new Vector3(SymbolPlaces[pos][2][1] * -1, SymbolPlaces[pos][2][0] * -1, meshFilters[0].transform.localPosition.z);
                        meshFilters[2].transform.localPosition = new Vector3(SymbolPlaces[pos][0][1] * -1, SymbolPlaces[pos][0][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[3].transform.localPosition = new Vector3(SymbolPlaces[pos][2][1] * -1, SymbolPlaces[pos][2][0], meshFilters[0].transform.localPosition.z);
                        meshFilters[4].transform.localPosition = new Vector3(SymbolPlaces[pos][4][1] * -1, SymbolPlaces[pos][4][0], meshFilters[0].transform.localPosition.z);
                        break;
                }
                break;
        }
    }

    void ButtonPress(int pos)
    {
        if (pos < 2 && !Pressing[pos])
        {
            StartCoroutine(RotateCog(pos));
            Audio.PlaySoundAtTransform("turn", Buttons[pos].transform);
            Positions[pos] = (Positions[pos] + 1) % 4;
        }
        else if (pos == 2)
        {
            try
            {
                StopCoroutine(ButtonPressAnim);
            }
            catch { }
            ButtonPressAnim = StartCoroutine(SubmitButtonAnim());
            Audio.PlaySoundAtTransform("press", Buttons[pos].transform);
            if (Positions.Sum() == 0 && !Solved)
            {
                Module.HandlePass();
                StartCoroutine(SolveAnim());
                Audio.PlaySoundAtTransform("solve", Buttons[2].transform);
                Debug.LogFormat("[Dial Trial #{0}] You submitted the correct permutation of the dials. Module solved!", _moduleID);
                for (int i = 0; i < 4; i++)
                    foreach (MeshRenderer symbol in BaseSymbolParents[i].GetComponentsInChildren<MeshRenderer>())
                        symbol.material.color = new Color(0, 0, 0, 1);
                Solved = true;
            }
            else if (!Solved)
            {
                Module.HandleStrike();
                Debug.LogFormat("[Dial Trial #{0}] You submitted an incorrect permutation of the dials. Strike!", _moduleID);
            }
        }
    }

    private IEnumerator RotateCog(int pos, float duration = 0.1f)
    {
        Pressing[pos] = true;
        float timer = 0;
        Vector3 start = Buttons[pos].transform.localEulerAngles;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[pos].transform.localEulerAngles = Vector3.Lerp(start, new Vector3(start.x, start.y, start.z + 90), timer * (1 / duration));
        }
        Buttons[pos].transform.localEulerAngles = new Vector3(start.x, start.y, (start.z + 90) % 360);
        Pressing[pos] = false;
    }

    private IEnumerator SubmitButtonAnim(float duration = 0.05f, float depression = 0.001f)
    {
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[2].transform.localPosition = Vector3.Lerp(SubmitButtonPos, new Vector3(SubmitButtonPos.x, SubmitButtonPos.y - depression, SubmitButtonPos.z), timer * (1 / duration));
        }
        Buttons[2].transform.localPosition = new Vector3(SubmitButtonPos.x, SubmitButtonPos.y - depression, SubmitButtonPos.z);
        timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[2].transform.localPosition = Vector3.Lerp(new Vector3(SubmitButtonPos.x, SubmitButtonPos.y - depression, SubmitButtonPos.z), SubmitButtonPos, timer * (1 / duration));
        }
        Buttons[2].transform.localPosition = SubmitButtonPos;
    }

    private IEnumerator SolveAnim(float duration = 1f, float endScale = 4f / 3)
    {
        float startScale = Steam.transform.localScale.x;
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            float scale = Easing.OutExpo(timer, startScale, startScale * endScale, duration);
            Steam.transform.localScale = new Vector3(scale, scale, scale);
            Steam.color = new Color(Steam.color.r, Steam.color.g, Steam.color.b, Mathf.Lerp(1, 0, timer / duration));
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} 12*' to spin the outer dial, then the inner dial, then press the submit button.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        for (int i = 0; i < command.Length; i++)
            if (!"12*".Contains(command[i]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
        yield return null;
        for (int i = 0; i < command.Length; i++)
        {
            Buttons["12*".IndexOf(command[i])].OnInteract();
            if (i < command.Length - 1)
            {
                if (command[i + 1] != command[i])
                {
                    float timer = 0;
                    while (timer < 0.1f)
                    {
                        yield return null;
                        timer += Time.deltaTime;
                    }
                }
                else
                    yield return new WaitWhile(() => Pressing["12*".IndexOf(command[i])]);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        somethingWentWrong:
        for (int i = 0; i < 2; i++)
            while (Positions[i] != 0)
            {
                Buttons[i].OnInteract();
                while (Pressing[i] && Positions[i] != 0)
                    yield return true;
            }
        if (Positions.Sum() == 0)
            Buttons[2].OnInteract();
        else
            goto somethingWentWrong;
    }
}
