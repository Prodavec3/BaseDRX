using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;

namespace Sungero.RecordManagement.Client
{
  partial class AcquaintanceTaskFunctions
  {
    /// <summary>
    /// Проверка, входит ли документ в список редактируемых форматов.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если входит, иначе False.</returns>
    public virtual bool IsEditableDocumentFormat(Docflow.IOfficialDocument document)
    {
      var whiteList = new List<string>() { "doc", "docx", "xls", "xlsx", "rtf", "odt", "ods", "txt" };
      if (document.AssociatedApplication == null)
        return false;
      
      return whiteList.Contains(document.AssociatedApplication.Extension.ToLower());
    }
    
    /// <summary>
    /// Проверка, нужно ли рекомендовать подписать документ перед ознакомлением.
    /// </summary>
    /// <param name="isElectronicAcquaintance">Значение галочки "В электронном виде".</param>
    /// <param name="document">Вложенный документ.</param>
    /// <returns>True, если документу требуется утверждающая подпись, иначе False.</returns>
    [Public]
    public virtual bool NeedShowSignRecommendation(bool isElectronicAcquaintance, Docflow.IOfficialDocument document)
    {
      // Проверка актуальна только для черновиков и электронного ознакомления.
      var isDraft = _obj.Status.Value == Status.Draft;
      if (!isDraft || !isElectronicAcquaintance)
        return false;
      
      // Нет тела - проверка не нужна.
      if (document == null || !document.HasVersions)
        return false;
      
      // Проверить подпись только по белому списку.
      var inWhiteList = this.IsEditableDocumentFormat(document);
      var hasApprovalSignatures = Signatures.Get(document.LastVersion).Any(x => x.SignatureType == SignatureType.Approval);
      return !hasApprovalSignatures && inWhiteList;
    }
    
  }
}