using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ContractualDocumentBase;

namespace Sungero.Docflow.Server
{
  partial class ContractualDocumentBaseFunctions
  {
    /// <summary>
    /// Получить права подписания договорных документов.
    /// </summary>
    /// <returns>Список подходящих правил.</returns>
    [Obsolete("Используйте метод GetSignatureSettingsQuery")]
    public override List<ISignatureSetting> GetSignatureSettings()
    {
      var basedSettings = base.GetSignatureSettings()
        .Where(s => s.Limit == Docflow.SignatureSetting.Limit.NoLimit || (s.Limit == Docflow.SignatureSetting.Limit.Amount &&
                                                                          s.Amount >= _obj.TotalAmount && Equals(s.Currency, _obj.Currency)))
        .ToList();
      
      if (_obj.DocumentKind != null && _obj.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
      {
        var category = Docflow.PublicFunctions.OfficialDocument.GetDocumentGroup(_obj);
        basedSettings = basedSettings
          .Where(s => !s.Categories.Any() || s.Categories.Any(c => Equals(c.Category, category)))
          .ToList();
      }
      return basedSettings;
    }
    
    /// <summary>
    /// Получить права подписания договорных документов.
    /// </summary>
    /// <returns>Список подходящих правил.</returns>
    public override IQueryable<ISignatureSetting> GetSignatureSettingsQuery()
    {
      var basedSettings = base.GetSignatureSettingsQuery()
        .Where(s => s.Limit == Docflow.SignatureSetting.Limit.NoLimit || (s.Limit == Docflow.SignatureSetting.Limit.Amount &&
                                                                          s.Amount >= _obj.TotalAmount && Equals(s.Currency, _obj.Currency)));
      
      if (_obj.DocumentKind != null && _obj.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
      {
        var category = Docflow.PublicFunctions.OfficialDocument.GetDocumentGroup(_obj);
        basedSettings = basedSettings
          .Where(s => !s.Categories.Any() || s.Categories.Any(c => Equals(c.Category, category)));
      }
      return basedSettings;
    }
    
    /// <summary>
    /// Проверить, связан ли документ специализированной связью.
    /// </summary>
    /// <returns>True - если связан, иначе - false.</returns>
    [Remote(IsPure = true)]
    public override bool HasSpecifiedTypeRelations()
    {
      var hasSpecifiedTypeRelations = false;
      AccessRights.AllowRead(
        () =>
        {
          hasSpecifiedTypeRelations = AccountingDocumentBases.GetAll().Any(x => Equals(x.LeadingDocument, _obj));
        });
      return base.HasSpecifiedTypeRelations() || hasSpecifiedTypeRelations;
    }
  }
}