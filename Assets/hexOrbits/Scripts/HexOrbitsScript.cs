﻿using EmikBaseModules;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class HexOrbitsScript : ModuleScript
{
    internal override ModuleConfig ModuleConfig
    {
        get
        {
            return new ModuleConfig(kmBombModule: Module);
        }
    }

    public KMAudio Audio;
    public KMBombModule Module;
    public KMSelectable Screen;
    public KMSelectable[] Buttons;

    public Texture[] ShapeTextures;
    public Renderer FastShape, SlowShape, DecorCube;

    public TextMesh Text;
    //to read/write: Text.text

    private static int[] _tableValues = new int[64]
    {
        0,  1,  2,  15, 0,  5,  4,  14,
        14, 13, 6,  3,  6,  6,  8,  11,
        4,  1,  8,  1,  15, 9,  14, 10,
        15, 12, 12, 10, 9,  5,  14, 7,
        8,  1,  15, 0,  4,  12, 1,  7,
        5,  7,  2,  11, 14, 3,  5,  12,
        3,  14, 11, 4,  7,  10, 1,  7,
        15, 3,  9,  9,  4,  8,  15, 15
    };
    //this is the hexOrbits static array, generated by a program made by Emik. i can't believe this makes sense

    /*here's the array in hexadecimal for reference:
    012F054E
    ED63668B
    4181F9EA
    FCCA95E7
    81F04C17
    572BE35C
    3EB47A17
    F39948FF
    */

    // note to self - fields can only be initiated outside of functions, otherwise it's a local. to modify values, have them outside of any function. you will forget this, and look here to remember it
    internal int[] stageValues = new int[5];

    private int _step;

    private string[] _directions = new string[4] { "Up", "Right", "Down", "Left" };

    /// <summary>
    /// Stage counter for the module. (-1 = init, 0-3 = Stages)
    /// </summary>
    private int _stage = -1;

    private int _lastInput = -1, _stageDisplay = -1;

    private float _shrinkAnimate;

    private IEnum<int> ienum;

    /*fun ways to log for errors: 
     *  this.Log("{0} {1} {2}".Form(_lastInput, arg1, submission));
     *  this.Log(new[] { _lastInput, arg1, submission }.Join());
     */


    //base functions, made easy with emikbasemodules!

    private void Start()
    {
        ienum = new IEnum<int>(OnSubmit, this);

        Buttons.Assign(onInteract: HandleButtons);
        Screen.Assign(onInteract: HandleScreen);

        int locationSeed = Rnd.Range(0, _tableValues.Length),
            movementSeed = Rnd.Range(1, 5);
        this.Log("hexOrbits loaded! Starting at location {0}, with {1}-step movement.".Form(locationSeed + 1, movementSeed));

        //pointer locations are actually all predetermined from the start! the manual technically lies!

        for (int i = 0; i < stageValues.Length; i++)
        {
            stageValues[i] = _tableValues.ElementAtWrap(locationSeed + (movementSeed * i));
        }

        this.Log("The 4 array cells are: {0}, {1}, {2}, {3}. (with locations of: {4}, {5}, {6}, {7})".Form(stageValues[0], stageValues[1], stageValues[2], stageValues[3], locationSeed + 1, (locationSeed + movementSeed + 1) % 64, (locationSeed + (movementSeed * 2) + 1) % 64, (locationSeed + (movementSeed * 3) + 1) % 64));

        this.Log("This module's solution is {0} (in location {1}), and can be submitted by pressing {2}, {3}.".Form(ToHex(stageValues[4]), (locationSeed + (movementSeed * 4) + 1) % 64, _directions[stageValues[4] / 4], _directions[stageValues[4] % 4]));
    }

    private void FixedUpdate()
    {
        _step++;
        FastShape.transform.localRotation = Quaternion.Euler(0, _step * Mathf.PI, 0);
        SlowShape.transform.localRotation = Quaternion.Euler(0, _step, 0);
        DecorCube.transform.localRotation = Quaternion.Euler((_step * Mathf.PI) / 2.5f, _step, (_step * Mathf.PI) / 4f);
        if (_shrinkAnimate > 0)
        {
            _shrinkAnimate -= 0.0007f;
            FastShape.transform.localScale = new Vector3(_shrinkAnimate, 1, _shrinkAnimate);
            SlowShape.transform.localScale = new Vector3(_shrinkAnimate, 1, _shrinkAnimate);
        }
    }

    private bool HandleButtons(int arg1)
    {
        Buttons[arg1].Button(Audio, Buttons[arg1].transform, 1, "ButtonPress");
        if (IsSolve || ienum.IsRunning)
            return false;
        if (_lastInput != -1)
        {
            ienum.RunCoroutine(arg1);
        }
        else
        {
            _lastInput = arg1;
            Text.text = "Submitting... - {0}, ?".Form(_directions[_lastInput]);
        }
        return false;
    }
    
    private bool HandleScreen()
    {
        Screen.Button(Audio, Screen.transform, 1, _stageDisplay == -1 ? "Activate" : "Step");
        if (IsSolve)
            return false;
        _stage = ++_stage % 4;
        RenderCurrentStage();
        return false;
    }

    private IEnumerator OnSubmit(int arg1)
    {
        Text.text = "Submitting - {0}, {1}".Form(_directions[_lastInput], _directions[arg1]);
        Audio.Play(transform, "Anticipation");

        _shrinkAnimate = 0.07f;

        yield return new WaitForSecondsRealtime(2.5f);

        int submission = (_lastInput * 4) + arg1;

        if (submission == stageValues[4])
        {
            Text.text = "hexOrbits - Complete!";
            Audio.Play(transform, "Solve");

            SlowShape.material.mainTexture = ShapeTextures[0];
            FastShape.material.mainTexture = ShapeTextures[4];

            this.Log("Submitted {0} - Module solved!".Form(ToHex(submission)));
            IsSolve = true;
            Module.HandlePass();
        }
        else
        {
            Text.text = "hexOrbits - Error!";
            Audio.Play(transform, "Strike");

            this.Log("Submitted {0}, expected {1} - Strike!".Form(ToHex(submission), ToHex(stageValues[4])));
            Module.HandleStrike();
        }

        FastShape.transform.localScale = new Vector3(0.07f, 1, 0.07f);
        SlowShape.transform.localScale = new Vector3(0.07f, 1, 0.07f);

        _lastInput = -1;
    }

    private void RenderCurrentStage()
    {
        _stageDisplay = (_stageDisplay + 1) % 4;
        int currentStage = stageValues[_stage];
        SlowShape.material.mainTexture = ShapeTextures[currentStage / 4];
        FastShape.material.mainTexture = ShapeTextures[currentStage % 4];
        Text.text = "hexOrbits - Index {0} of 4".Form(_stageDisplay + 1);
    }


    private static string ToHex(int i)
    {
        return Convert.ToString(i, 16).ToUpperInvariant();
    }

}
 