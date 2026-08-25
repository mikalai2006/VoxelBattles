using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public static class HelperUI
{
  public static List<LocalizeObj> FindAllTextElements(VisualElement element)
  {
    List<LocalizeObj> elementList = new List<LocalizeObj>();
    if (typeof(TextElement).IsInstanceOfType(element))
    {
      var textElement = (TextElement)element;
      var key = textElement.text;
      if (!string.IsNullOrEmpty(textElement.viewDataKey) && textElement.viewDataKey[0] == '#')
      {
        key = textElement.viewDataKey;
      }
      if (!string.IsNullOrEmpty(key) && key[0] == '#')
      {
        textElement.viewDataKey = key;
        key = key.TrimStart('#');
        elementList.Add(new LocalizeObj
        {
          Element = textElement,
          Key = key
        });
      }
    }
    //if have child
    var hierarchy = element.hierarchy;
    var childs = hierarchy.childCount;
    for (int i = 0; i < childs; i++)
    {
      elementList.AddRange(FindAllTextElements(hierarchy.ElementAt(i)));
    }

    return elementList;
  }
}

[Serializable]
public class LocalizeObj
{
  public string Key;
  public TextElement Element;
}