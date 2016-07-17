﻿// winifix@gmail.com
// ReSharper disable UnusedMember.Global
// ReSharper disable ConvertPropertyToExpressionBody

using PixelMagic.Helpers;
using System.Drawing;
using System.Threading;

namespace PixelMagic.Rotation
{
    public class Warrior : CombatRoutine
    {
        public override string Name
        {
            get
            {
                return "Warrior Sample";
            }
        }

        public override string Class
        {
            get
            {
                return "Warrior";
            }
        } 

        public override void Initialize()
        {
            Log.Write("Welcome to Warrior Sample", Color.Green);
        }

        public override void Stop()
        {
        }

        public override void Pulse()
        {   
			if (combatRoutine.Type == RotationType.SingleTarget)  // Do Single Target Stuff here
            {
                if (WoW.HasTarget && WoW.TargetIsEnemy)
                {
                    Log.Write("Aura Count: " + WoW.GetAuraCount("Taste for Blood"));
                }
            }
            if (combatRoutine.Type == RotationType.AOE)
            {
                // Do AOE Stuff here                
            }            
        }
    }
}

/*
[AddonDetails.db]
AddonAuthor=Warrior
AddonName=PixelMagic
WoWVersion=Legion - 70000
[SpellBook.db]
Spell,100,Charge, D1
Aura,24604,Taste for Blood
*/
