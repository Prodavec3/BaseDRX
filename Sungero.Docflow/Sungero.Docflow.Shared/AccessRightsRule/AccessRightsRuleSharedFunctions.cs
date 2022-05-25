using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow.Shared
{
  partial class AccessRightsRuleFunctions
  {
    /// <summary>
    /// Получить доступные категории договоров.
    /// </summary>
    /// <returns>Список категорий договоров.</returns>
    public virtual List<IDocumentGroupBase> GetDocumentGroups()
    {
      var kinds = _obj.DocumentKinds.Select(k => k.DocumentKind).ToList();
      var contractKinds = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(Contracts.IContractBase)).ToList();
      if (kinds.Any() && kinds.All(k => contractKinds.Contains(k)))
        return Contracts.PublicFunctions.ContractCategory.GetFilteredContractCategoris(kinds);
      return new List<IDocumentGroupBase>();
    }
  }
}