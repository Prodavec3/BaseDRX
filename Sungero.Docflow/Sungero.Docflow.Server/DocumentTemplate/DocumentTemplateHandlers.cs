using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentTemplate;
using Sungero.Domain.Shared;

namespace Sungero.Docflow
{
  partial class DocumentTemplateServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Status = Docflow.DocumentTemplate.Status.Active;
    }
  }

  partial class DocumentTemplateFilteringServerHandler<T>
  {
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      // Фильтр при создании документа из шаблона.
      if (_createFromTemplateContext != null)
      {
        // Фильтр по состоянию.
        query = query.Where(d => d.Status == Status.Active);
        
        // Фильтр по критериям.
        if (Docflow.OfficialDocuments.Is(_createFromTemplateContext))
        {
          var document = Docflow.OfficialDocuments.As(_createFromTemplateContext);
          query = FilterTemplatesByCriteria(query,
                                            document,
                                            document.DocumentKind,
                                            document.BusinessUnit,
                                            document.Department,
                                            true);
        }
      }
      else if (_filter != null)
      {
        // Фильтр в списке.
        // Фильтр по состоянию.
        if (_filter.Active != _filter.Closed)
          query = query.Where(d => _filter.Active && d.Status == Status.Active ||
                              _filter.Closed && d.Status == Status.Closed);
        
        // Фильтр по критериям.
        query = FilterTemplatesByCriteria(query,
                                          null,
                                          _filter.DocumentKind,
                                          _filter.BusinessUnit,
                                          _filter.Department,
                                          false);
      }
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать шаблоны по критериям.
    /// </summary>
    /// <param name="documentTemplates">Коллекция шаблонов.</param>
    /// <param name="document">Документ, который создается из шаблона.</param>
    /// <param name="documentKind">Вид документа.</param>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="isCalledFromDocument">True - вызывается из документа, false - вызывается из списка.</param>
    /// <returns>Коллекция отфильтрованных шаблонов.</returns>
    public static IQueryable<T> FilterTemplatesByCriteria(IQueryable<T> documentTemplates,
                                                          IOfficialDocument document,
                                                          IDocumentKind documentKind,
                                                          Company.IBusinessUnit businessUnit,
                                                          Company.IDepartment department,
                                                          bool isCalledFromDocument)
    {
      // Вид документа.
      var allDocumentTypeGuid = Guid.Parse(Docflow.PublicConstants.DocumentTemplate.AllDocumentTypeGuid);
      if (documentKind != null)
      {
        var typeGuid = Guid.Parse(documentKind.DocumentType.DocumentTypeGuid);
        documentTemplates = documentTemplates.Where(d => !d.DocumentKinds.Any() &&
                                                    (d.DocumentType == allDocumentTypeGuid ||
                                                     d.DocumentType == typeGuid) ||
                                                    d.DocumentKinds.Any(k => Equals(k.DocumentKind, documentKind)));
      }
      else if (isCalledFromDocument)
      {
        var typeGuid = document.TypeDiscriminator;
        var availableKinds = PublicFunctions.DocumentKind.GetAvailableDocumentKinds(document);
        documentTemplates = documentTemplates.Where(d => !d.DocumentKinds.Any() &&
                                                    (d.DocumentType == allDocumentTypeGuid ||
                                                     d.DocumentType == typeGuid) ||
                                                    availableKinds.Any(x => string.Equals(x.DocumentType.DocumentTypeGuid,
                                                                                          typeGuid.ToString(),
                                                                                          StringComparison.InvariantCultureIgnoreCase)));
      }
      
      // НОР.
      if (businessUnit != null)
        documentTemplates = documentTemplates.Where(d => !d.BusinessUnits.Any() || d.BusinessUnits.Any(t => Equals(t.BusinessUnit, businessUnit)));
      else if (isCalledFromDocument)
        documentTemplates = documentTemplates.Where(d => !d.BusinessUnits.Any());
      
      // Подразделение.
      if (department != null)
        documentTemplates = documentTemplates.Where(d => !d.Departments.Any() || d.Departments.Any(t => Equals(t.Department, department)));
      else if (isCalledFromDocument)
        documentTemplates = documentTemplates.Where(d => !d.Departments.Any());
      
      return documentTemplates;
    }
  }

  partial class DocumentTemplateDocumentKindsDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindsDocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_root.DocumentType == null ||
          string.Equals(_root.DocumentType.ToString(), Docflow.PublicConstants.DocumentTemplate.AllDocumentTypeGuid, StringComparison.InvariantCultureIgnoreCase))
        return query;
      
      return query.Where(x => x.DocumentType.DocumentTypeGuid == _root.DocumentType.ToString());
    }
  }
}