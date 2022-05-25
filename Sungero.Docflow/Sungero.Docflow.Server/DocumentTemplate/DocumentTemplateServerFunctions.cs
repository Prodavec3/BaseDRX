using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentTemplate;

namespace Sungero.Docflow.Server
{
  partial class DocumentTemplateFunctions
  {
    /// <summary>
    /// Получить действующие шаблоны по виду документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Шаблоны.</returns>
    [Public]
    public static IQueryable<IDocumentTemplate> GetDocumentTemplatesByDocumentKind(IDocumentKind documentKind)
    {
      return DocumentTemplates.GetAll()
        .Where(d => d.Status == Docflow.DocumentTemplate.Status.Active)
        .Where(d => d.DocumentKinds.Any(k => Equals(k.DocumentKind, documentKind)));
    }
    
  }
}