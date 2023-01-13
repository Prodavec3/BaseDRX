using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ExchangeCore.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Проставить статус соединения в абонентских ящиках подразделений.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    public void SetDepartmentBoxConnectionStatus(IBusinessUnitBox box)
    {
      var departmentBoxes = Functions.BoxBase.Remote.GetActiveChildBoxes(box);
      var closedDepartmentBoxes = Functions.BoxBase.Remote.GetClosedChildBoxes(box);
      departmentBoxes.AddRange(closedDepartmentBoxes);
      departmentBoxes = departmentBoxes.Where(x => !Equals(x.ConnectionStatus, box.ConnectionStatus)).ToList();
      foreach (var departmentBox in departmentBoxes)
      {
        if (!Equals(departmentBox.ConnectionStatus, box.ConnectionStatus))
        {
          departmentBox.ConnectionStatus = box.ConnectionStatus;
          departmentBox.Save();
        }
      }
    }
    
    /// <summary>
    /// Получить значение параметра из гиперссылки.
    /// </summary>
    /// <param name="hyperlink">Гиперссылка на документ из сервиса обмена.</param>
    /// <param name="parameterName">Имя параметра.</param>
    /// <returns>Значение параметра.</returns>
    public virtual string GetParameterValueFromHyperlink(string hyperlink, string parameterName)
    {
      if (string.IsNullOrWhiteSpace(hyperlink) || string.IsNullOrWhiteSpace(parameterName))
        return string.Empty;
      
      try
      {
        Uri hyperlinkUri = new Uri(hyperlink);
        return System.Web.HttpUtility.ParseQueryString(hyperlinkUri.Query).Get(parameterName);
      }
      catch
      {
        return string.Empty;
      }
    }
    
    /// <summary>
    /// Получить Guid документа из гиперссылки.
    /// </summary>
    /// <param name="hyperlink">Гиперссылка на документ из сервиса обмена.</param>
    /// <returns>Строковое представление Guid документа.</returns>
    public virtual string GetDocumentGuidFromHyperlink(string hyperlink)
    {
      if (string.IsNullOrWhiteSpace(hyperlink))
        return string.Empty;
      
      var documentAddressParts = hyperlink.Split(Constants.Module.HyperlinkDelimiter);
      var pattern = Constants.Module.GuidPatternWithAdditionalInfo;
      foreach (var part in documentAddressParts)
      {
        var match = System.Text.RegularExpressions.Regex.Match(part, pattern);
        if (match.Success)
          return match.Value;
      }
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить, что строка соответствует формату Guid.
    /// </summary>
    /// <param name="guid">Строка, содержащая Guid.</param>
    /// <returns>True, если строка соответствует формату Guid, иначе False.</returns>
    public virtual bool CheckGuid(string guid)
    {
      if (string.IsNullOrWhiteSpace(guid))
        return false;
      
      var pattern = Constants.Module.GuidPattern;
      var match = System.Text.RegularExpressions.Regex.Match(guid, pattern);
      if (match.Success)
        return true;

      return false;
    }
  }
}