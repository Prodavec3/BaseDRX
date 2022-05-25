using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow
{
  partial class AccessRightsRuleFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Active || _filter.Closed)
        query = query.Where(r => _filter.Active && r.Status == Status.Active || _filter.Closed && r.Status == Status.Closed);
      
      if (_filter.BusinessUnit != null)
        query = query.Where(r => !r.BusinessUnits.Any() || r.BusinessUnits.Any(u => Equals(u.BusinessUnit, _filter.BusinessUnit)));
      
      if (_filter.Department != null)
        query = query.Where(r => !r.Departments.Any() || r.Departments.Any(u => Equals(u.Department, _filter.Department)));
      
      if (_filter.DocumentKind != null)
        query = query.Where(r => !r.DocumentKinds.Any() || r.DocumentKinds.Any(u => Equals(u.DocumentKind, _filter.DocumentKind)));
      
      return query;
    }
  }

  partial class AccessRightsRuleDocumentGroupsDocumentGroupPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentGroupsDocumentGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var groups = Functions.AccessRightsRule.GetDocumentGroups(_root);
      return query.Where(g => groups.Contains(g));
    }
  }

  partial class AccessRightsRuleServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      if (_obj.GrantRightsOnExistingDocuments == true && _obj.Status == Status.Active)
      {
        PublicFunctions.Module.CreateGrantAccessRightsToDocumentsByRuleAsyncHandler(_obj.Id);
      }      
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      var ruleItems = DocumentGrantRightsQueueItems.GetAll(q => Equals(q.AccessRightsRule, _obj)).ToList();
      foreach (var item in ruleItems)
        DocumentGrantRightsQueueItems.Delete(item);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (Equals(_obj.Status, Status.Closed))
        return;
      
      if (!_obj.DocumentKinds.Any() &&
          !_obj.BusinessUnits.Any() &&
          !_obj.Departments.Any() &&
          !_obj.DocumentGroups.Any())
      {
        e.AddError(AccessRightsRules.Resources.RuleMustHaveCriteria);
      }
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      _obj.Modified = Calendar.Now;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (_obj.GrantRightsOnLeadingDocument == null)
        _obj.GrantRightsOnLeadingDocument = false;
      
      if (_obj.GrantRightsOnExistingDocuments == null)
        _obj.GrantRightsOnExistingDocuments = true;
      
      _obj.Modified = Calendar.Now;
    }
  }

}