using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractCategory;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Contracts.Shared
{
  partial class ContractCategoryFunctions
  {
    /// <summary>
    /// Получить список доступных видов документов для категорий.
    /// </summary>
    /// <returns>Виды документов.</returns>
    [Public]
    public static List<Docflow.IDocumentKind> GetAllowedDocumentKinds()
    {
      return Docflow.PublicFunctions.DocumentKind.GetAvailableDocumentKinds(typeof(IContractBase))
        .Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(d => d.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
        .ToList();
    }

    /// <summary>
    /// Получить список групп документов, доступных для выбора в правиле.
    /// </summary>
    /// <param name="documentKinds">Виды документов, для которых подбираются группы документов.</param>
    /// <returns>Список групп документов.</returns>
    [Public]
    public static List<Docflow.IDocumentGroupBase> GetFilteredContractCategoris(List<Docflow.IDocumentKind> documentKinds)
    {
      var filtrableDocumentKinds = Functions.ContractCategory.GetAllowedDocumentKinds();
      var filtrableDocumentKindsInRule = documentKinds.Where(dk => filtrableDocumentKinds.Contains(dk)).ToList();
      
      var documentGroups = Docflow.DocumentGroupBases.GetAllCached().ToList();
      if (filtrableDocumentKindsInRule.Any())
        for (int i = 0; i < documentGroups.Count; i++)
      {
        var groupDocumentKinds = documentGroups[i].DocumentKinds.Select(d => d.DocumentKind).ToList();
        
        if (groupDocumentKinds.Any() && groupDocumentKinds.Where(dk => filtrableDocumentKindsInRule.Contains(dk)).Count() != filtrableDocumentKindsInRule.Count())
        {
          documentGroups.RemoveAt(i);
          i--;
        }
      }
      return documentGroups;
    }
  }
}