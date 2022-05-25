using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentKind;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace Sungero.Docflow.Shared
{
  partial class DocumentKindFunctions
  {
    /// <summary>
    /// Получить срок рассмотрения документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <param name="user">Пользователь.</param>
    /// <returns>Срок рассмотрения.</returns>
    [Public]
    public static DateTime? GetConsiderationDate(IDocumentKind documentKind, IUser user)
    {
      if (documentKind == null || (!documentKind.DeadlineInDays.HasValue && !documentKind.DeadlineInHours.HasValue))
        return null;
      return Calendar.Now.AddWorkingDays(user, documentKind.DeadlineInDays ?? 0).AddWorkingHours(user, documentKind.DeadlineInHours ?? 0);
    }
    
    /// <summary>
    /// Получить доступные виды документов.
    /// </summary>
    /// <param name="documentType">Тип документа, передавать как typeof(IOfficialDocument).</param>
    /// <returns>Доступные виды.</returns>
    /// <remarks>Должно работать с наследниками на слоях, т.е. возвращать и их.</remarks>
    [Public]
    public static IQueryable<IDocumentKind> GetAvailableDocumentKinds(System.Type documentType)
    {
      var guids = GetDocumentGuids(documentType);
      return Functions.DocumentKind.Remote.GetAllDocumentKinds().Where(d => d.DocumentType != null && guids.Contains(d.DocumentType.DocumentTypeGuid));
    }
    
    /// <summary>
    /// Получить гуиды для типа документа и его наследников.
    /// </summary>
    /// <param name="documentType">Тип документа, передавать как typeof(IOfficialDocument).</param>
    /// <returns>Доступные гуиды, в том числе наследники.</returns>
    [Public]
    public static List<string> GetDocumentGuids(System.Type documentType)
    {
      var metadata = documentType.GetEntityMetadata();
      var entities = metadata.DescendantEntities.ToList();
      entities.Add(metadata);
      var guids = entities.Select(m => m.GetOriginal()).Distinct()
        .Where(de => !de.IsAbstract)
        .Select(de => de.NameGuid.ToString())
        .ToList();
      return guids;
    }
  }
}