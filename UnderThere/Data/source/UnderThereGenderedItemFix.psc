Scriptname UnderThereGenderedItemFix extends ActiveMagicEffect

Event OnEffectStart(Actor akTarget, Actor akCaster)
    FormList armorsToRemove = None;
    int gender = akCaster.GetLeveledActorBase().GetSex();
    If (gender == 0) ; caster is male
        armorsToRemove = femaleItems;
    ElseIf (gender == 1) ; caster is female
        armorsToRemove = maleItems;
    EndIf
    
    If (armorsToRemove)
        Form[] armorsList = armorsToRemove.ToArray()
        int i = armorsList.Length
        While (i)
            i -= 1;
            akCaster.removeItem(armorsList[i], 2, True)
        EndWhile
    EndIf
EndEvent

FormList Property femaleItems Auto
FormList Property maleItems Auto