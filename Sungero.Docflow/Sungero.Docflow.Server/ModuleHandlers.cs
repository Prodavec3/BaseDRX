using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRuleBase;

namespace Sungero.Docflow.Server
{

  partial class StoragePolicyFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IStoragePolicyBase> StoragePolicyDataQuery(IQueryable<Sungero.Docflow.IStoragePolicyBase> query)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Active || _filter.Closed)
        query = query.Where(r => _filter.Active && r.Status == Status.Active ||
                            _filter.Closed && r.Status == Status.Closed);
      
      if (_filter.DocumentKind != null)
        query = query.Where(r => !r.DocumentKinds.Any() || r.DocumentKinds.Any(u => Equals(u.DocumentKind, _filter.DocumentKind)));
      
      if (_filter.StoragePolicy || _filter.RetentionPolicy)
        query = query.Where(r => StoragePolicies.Is(r) && _filter.StoragePolicy ||
                            RetentionPolicies.Is(r) && _filter.RetentionPolicy);

      return query;
    }
  }

  partial class ApprovalRulesFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IApprovalRuleBase> ApprovalRulesDataQuery(IQueryable<Sungero.Docflow.IApprovalRuleBase> query)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Active || _filter.Closed || _filter.Draft)
        query = query.Where(r => _filter.Active && r.Status == Status.Active ||
                            _filter.Closed && r.Status == Status.Closed ||
                            _filter.Draft && r.Status == Status.Draft);
      
      if (_filter.BusinessUnit != null)
        query = query.Where(r => !r.BusinessUnits.Any() || r.BusinessUnits.Any(u => Equals(u.BusinessUnit, _filter.BusinessUnit)));
      
      if (_filter.Department != null)
        query = query.Where(r => !r.Departments.Any() || r.Departments.Any(u => Equals(u.Department, _filter.Department)));
      
      if (_filter.DocumentKind != null)
      {
        query = query.Where(r => r.DocumentFlow == _filter.DocumentKind.DocumentType.DocumentFlow);
        query = query.Where(r => !r.DocumentKinds.Any() || r.DocumentKinds.Any(u => Equals(u.DocumentKind, _filter.DocumentKind)));
      }
      
      return query;
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ApprovalRulesDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      var sendAction = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendForApproval);
      return query.Where(d => d.AvailableActions.Any(a => Equals(a.Action, sendAction)));
    }
  }
}