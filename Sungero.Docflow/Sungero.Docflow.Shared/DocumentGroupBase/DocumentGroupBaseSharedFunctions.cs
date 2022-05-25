using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentGroupBase;

namespace Sungero.Docflow.Shared
{
  partial class DocumentGroupBaseFunctions
  {
    /// <summary>
    /// Получить доступные группы документов по видам документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Группы документов.</returns>
    [Public]
    public static IQueryable<IDocumentGroupBase> GetAvailableDocumentGroup(IDocumentKind documentKind)
    {
      return DocumentGroupBases.GetAllCached()
        .Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(d => !d.DocumentKinds.Any() || d.DocumentKinds.Any(k => Equals(k.DocumentKind, documentKind)))
        .AsQueryable();
    }
  }
}