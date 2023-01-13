using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ConditionBase;

namespace Sungero.Docflow
{
  partial class ConditionBaseRecipientForComparisonPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> RecipientForComparisonFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(q => q.Status == CoreEntities.DatabookEntry.Status.Active);
      
      // Для выбора доступны только сотрудники и одиночные роли.
      return query.Where(q => Company.Employees.Is(q) || Roles.Is(q) && Roles.As(q).IsSingleUser == true);
    }
  }

  partial class ConditionBaseApprovalRoleForComparisonPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ApprovalRoleForComparisonFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var multipleMembersRoles = Docflow.Functions.Module.GetMultipleMembersRoles();
      return query.Where(r => !multipleMembersRoles.Contains(r.Type) && !Equals(r, _obj.ApprovalRole))
        .Where(r => r.Type != Docflow.ApprovalRoleBase.Type.PrintResp);
    }
  }

  partial class ConditionBaseApprovalRolePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ApprovalRoleFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var multipleMembersRoles = Docflow.Functions.Module.GetMultipleMembersRoles();

      if (_obj.ConditionType == Docflow.ConditionBase.ConditionType.EmployeeInRole)
        return query.Where(r => multipleMembersRoles.Contains(r.Type) && r.Type != Docflow.ApprovalRoleBase.Type.Approvers);

      return query.Where(r => !multipleMembersRoles.Contains(r.Type) && !Equals(r, _obj.ApprovalRoleForComparison))
        .Where(r => r.Type != Docflow.ApprovalRoleBase.Type.PrintResp);
    }
  }

  partial class ConditionBaseConditionDocumentKindsDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ConditionDocumentKindsDocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var sendAction = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendForApproval);
      if (_root.DocumentKinds.Any())
      {
        var availableDocumentKind = _root.DocumentKinds.Select(k => k.DocumentKind).ToList();
        query = query.Where(q => availableDocumentKind.Contains(q));
      }
      return query.Where(d => d.AvailableActions.Any(a => Equals(a.Action, sendAction)));
    }
  }

  partial class ConditionBaseServerHandlers
  {
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.State.Properties.ConditionType.IsChanged && Functions.ConditionBase.HasRules(_obj))
        e.AddError(ConditionBases.Resources.ConditionHasRules);
      
      var conditionName = Functions.ConditionBase.GetConditionName(_obj);
      _obj.Name = Functions.Module.TrimSpecialSymbols(conditionName);
      
      if (_obj.Name.Length >= 250)
        _obj.Name = _obj.Name.Remove(250);
    }
  }
}