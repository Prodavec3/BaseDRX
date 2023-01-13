using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalManagerAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalManagerAssignmentReworkPerformerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ReworkPerformerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var reworkPerformersIds = Functions.ApprovalTask.GetReworkPerformers(ApprovalTasks.As(_obj.Task))
        .Select(p => p.Id).ToList();
      return query.Where(x => reworkPerformersIds.Contains(x.Id));
    }
  }

  partial class ApprovalManagerAssignmentAddApproversApproverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddApproversApproverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(app => Company.Employees.Is(app));
    }
  }

  partial class ApprovalManagerAssignmentExchangeServicePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ExchangeServiceFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var services = Functions.ApprovalTask.GetExchangeServices(ApprovalTasks.As(_obj.Task)).Services;
      query = query.Where(s => services.Contains(s));
      return query;
    }
  }

  partial class ApprovalManagerAssignmentSignatoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      if (Functions.OfficialDocument.SignatorySettingWithAllUsersExist(document))
        return query;
      
      var signatories = Functions.OfficialDocument.GetSignatoriesIds(document);
      
      return query.Where(s => signatories.Contains(s.Id));
    }
  }

  partial class ApprovalManagerAssignmentServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      Functions.ApprovalManagerAssignment.FillAddresseeFromAddressees(_obj);
    }

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result == Result.Approved)
        e.Result = ApprovalTasks.Resources.Endorsed;
      else if (_obj.Result == Result.WithSuggestions)
        e.Result = ApprovalTasks.Resources.EndorsedWithSuggestions;
      else
        e.Result = ApprovalTasks.Resources.ForRework;
    }
  }
}