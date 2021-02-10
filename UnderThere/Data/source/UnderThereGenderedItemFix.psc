Scriptname UnderThereGenderedItemFix extends ActiveMagicEffect

Event OnEffectStart(Actor akTarget, Actor akCaster)
    int gender = akCaster.GetLeveledActorBase().GetSex();
    If (gender == 0) ; caster is male
        RemoveItems(akCaster, femaleItems);
    ElseIf (gender == 1) ; caster is female
        RemoveItems(akCaster, maleItems);
    EndIf
EndEvent
    
Function RemoveItems(Actor target, FormList itemList)
    int i = itemList.Length
    While (i)
        i -= 1;
        target.removeItem(itemList.GetAt(i), 1, True)
    EndWhile
EndFunction

FormList Property femaleItems Auto
FormList Property maleItems Auto