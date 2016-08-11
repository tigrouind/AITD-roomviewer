using System;
using UnityEngine;

public class Vars : MonoBehaviour
{
    public MenuStyle Style;

    private bool pauseVarsTracking;

    private byte[] varsMemory = new byte[207*2];
    private byte[] oldVarsMemory = new byte[207*2];
    private float[] varsMemoryTime = new float[207];
    private bool[] varsDifference = new bool[207];
    private byte[] varsMemoryPattern = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2E, 0x00, 0x2F, 0x00, 0x00, 0x00, 0x00 }; 
    private long varsMemoryAddress = -1;

    private byte[] cvarsMemory = new byte[44*2];
    private byte[] oldCVarsMemory = new byte[44*2];
    private float[] cvarsMemoryTime = new float[44];
    private bool[] cvarsDifference = new bool[44];
    private byte[] cvarsMemoryPattern = new byte[] { 0x31, 0x00, 0x0E, 0x01, 0xBC, 0x02, 0x12, 0x00, 0x06, 0x00, 0x13, 0x00, 0x14, 0x00, 0x01 }; 
    private long cvarsMemoryAddress = -1;

    private short ReadShort(byte a, byte b)
    {
        unchecked
        {
            return (short)(a | b << 8);
        }
    }

    void Update()
    {
        ProcessMemoryReader processReader = GetComponent<DosBox>().ProcessReader;
        if (processReader != null)
        {
            if (!pauseVarsTracking)
            {
                if (varsMemoryAddress != -1)
                {
                    processReader.Read(varsMemory, varsMemoryAddress, varsMemory.Length);
                    CheckDifferences(varsMemory, oldVarsMemory, varsMemoryTime, varsDifference, 207);
                }

                if (cvarsMemoryAddress != -1)
                {
                    processReader.Read(cvarsMemory, cvarsMemoryAddress, cvarsMemory.Length);
                    CheckDifferences(cvarsMemory, oldCVarsMemory, cvarsMemoryTime, cvarsDifference, 44);
                }
            }
        }

        //freeze vars tracking
        if (Input.GetMouseButtonDown(0))
        {
            pauseVarsTracking = !pauseVarsTracking;
        }

        //hide table
        if (Input.GetMouseButtonDown(1))
        {
            pauseVarsTracking = false;
            GetComponent<Vars>().enabled = false;
        }
    }

    void OnGUI()
    {
        GUIStyle panel = new GUIStyle(Style.Panel);
        panel.normal.background = Style.BlackTexture;
        Rect areaA = new Rect(0, 0, Screen.width, Screen.height * 22.0f/28.0f);
        Rect areaB = new Rect(0, Screen.height * 22.0f/28.0f, Screen.width, Screen.height * 6.0f/28.0f);

        GUILayout.BeginArea(areaA, panel);
        DisplayTable(areaA, 10, 21, varsMemory, varsDifference, "VARS");
        GUILayout.EndArea();

        GUILayout.BeginArea(areaB, panel);
        DisplayTable(areaB, 10, 5, cvarsMemory, cvarsDifference, "CVARS");
        GUILayout.EndArea();
    }

    void CheckDifferences(byte[] values, byte[] oldvalues, float[] time, bool[] difference, int count)
    {
        float currenttime = Time.time;
        for(int i = 0 ; i < count ; i++)
        {
            int value = ReadShort(values[i * 2 + 0], values[i * 2 + 1]);
            int oldValue = ReadShort(oldvalues[i * 2 + 0], oldvalues[i * 2 + 1]);
            if (value != oldValue)
            {
                time[i] = currenttime;
            }

            difference[i] = (currenttime - time[i]) < 5.0f;

            oldvalues[i * 2 + 0] = values[i * 2 + 0];
            oldvalues[i * 2 + 1] = values[i * 2 + 1];
        }
    }

    void DisplayTable(Rect area, int columns, int rows, byte[] values, bool[] difference, string title)
    {
        //setup style
        GUIStyle labelStyle = new GUIStyle(Style.Label);
        labelStyle.fixedWidth = area.width/(columns + 1);
        labelStyle.fixedHeight = area.height/((float)(rows + 1));
        labelStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle headerStyle = new GUIStyle(labelStyle);
        headerStyle.normal.textColor = Color.black;
        headerStyle.normal.background = pauseVarsTracking ? Style.RedTexture : Style.GreenTexture;

        //header
        GUILayout.BeginHorizontal();
        GUILayout.Label(title, headerStyle);
        for (int i = 0 ; i < columns ; i++)
        {
            GUILayout.Label(i.ToString(), headerStyle);
        }
        GUILayout.EndHorizontal();

        //body
        int count = 0;
        for (int i = 0 ; i < rows ; i++)
        {
            GUILayout.BeginHorizontal();
            headerStyle.alignment = TextAnchor.MiddleRight;
            GUILayout.Label(i.ToString(), headerStyle);

            for (int j = 0; j < columns; j++)
            {
                string stringValue = string.Empty;
                if (count < values.Length / 2)
                {
                    int value = ReadShort(values[count * 2 + 0], values[count * 2 + 1]);
                    bool different = difference[count];

                    if (value != 0 || different)
                        stringValue = value.ToString();

                    //highlight recently changed vars
                    labelStyle.normal.background = different ? Style.RedTexture : null;
                }

                count++;
                GUILayout.Label(stringValue, labelStyle);
            }
            GUILayout.EndHorizontal();
        }
    }

    public void SearchForPatterns(ProcessMemoryReader reader)
    {
        varsMemoryAddress = reader.SearchForBytePattern(varsMemoryPattern);
        cvarsMemoryAddress = reader.SearchForBytePattern(cvarsMemoryPattern);
    }


}