using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentGroupBase;

namespace Sungero.Docflow.Server
{
  partial class DocumentGroupBaseFunctions
  {
    /// <summary>
    /// Получить группы документов.
    /// </summary>
    /// <returns>Группы документов.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IDocumentGroupBase> GetDocumentGroups()
    {
      return DocumentGroupBases.GetAll().Where(d => d.Status == Status.Active).OrderBy(d => d.Name);
    }
    
    /// <summary>
    /// Получить действующие группы документов по виду документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Группы документов.</returns>
    [Public]
    public static IQueryable<IDocumentGroupBase> GetDocumentGroupsByDocumentKind(IDocumentKind documentKind)
    {
      return DocumentGroupBases.GetAll()
        .Where(d => d.Status == Docflow.DocumentGroupBase.Status.Active)
        .Where(d => d.DocumentKinds.Any(k => Equals(k.DocumentKind, documentKind)));
    }
  }
}