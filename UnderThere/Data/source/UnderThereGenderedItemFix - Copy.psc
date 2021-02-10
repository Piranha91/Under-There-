Scriptname UnderThereGenderedItemFix extends ActiveMagicEffect

FormList Property femaleItems Auto
FormList Property maleItems Auto

Event OnEffectStart(Actor akTarget, Actor akCaster)

	Debug.MessageBox("UnderThere Script has been called");

    FormList armorsToRemove = None;
    int gender = akCaster.GetLeveledActorBase().GetSex();
    If (gender == 0) ; caster is male
        armorsToRemove = femaleItems;
		Debug.Notification("Player is male");
    ElseIf (gender == 1) ; caster is female
        armorsToRemove = maleItems;
		Debug.Notification("Player is female");
    EndIf
    
    If (armorsToRemove)
        Form[] armorsList = armorsToRemove.ToArray()
        int i = armorsList.Length
        While (i)
            i -= 1;
            akCaster.removeItem(armorsList[i], 10, True)
        EndWhile
    EndIf
EndEvent

