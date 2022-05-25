using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BoxBase;

namespace Sungero.ExchangeCore
{

  partial class BoxBaseFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Active || _filter.Closed)
        query = query.Where(r => _filter.Active && r.Status == Status.Active ||
                            _filter.Closed && r.Status == Status.Closed);
      
      if (_filter.BusinessUnit != null)
      {
        query = query.Where(b =>
                            (BusinessUnitBoxes.Is(b) && Equals(BusinessUnitBoxes.As(b).BusinessUnit, _filter.BusinessUnit)) ||
                            (DepartmentBoxes.Is(b) && Equals(DepartmentBoxes.As(b).RootBox.BusinessUnit, _filter.BusinessUnit)));
      }
      
      if (_filter.ExchangeService != null)
      {
        query = query.Where(b =>
                            (BusinessUnitBoxes.Is(b) && Equals(BusinessUnitBoxes.As(b).ExchangeService, _filter.ExchangeService)) ||
                            (DepartmentBoxes.Is(b) && Equals(DepartmentBoxes.As(b).RootBox.ExchangeService, _filter.ExchangeService)));
      }
      
      return query;
    }
  }

  partial class BoxBaseServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Routing = BoxBase.Routing.BoxResponsible;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (e.Params.Contains(Constants.BoxBase.DisableSaveValidation))
        return;
      
      if (_obj.Status == BoxBase.Status.Closed && _obj.State.Properties.Status.OriginalValue == BoxBase.Status.Active &&
          Functions.BoxBase.GetActiveChildBoxes(_obj).Any())
        e.AddError(BoxBases.Resources.FindActiveChildBoxes, _obj.Info.Actions.FindActiveChildBoxes);
      
      if (_obj.Status == BoxBase.Status.Active && _obj.State.Properties.Status.OriginalValue == BoxBase.Status.Closed &&
          Functions.BoxBase.GetClosedChildBoxes(_obj).Any())
        e.AddInformation(BoxBases.Resources.FindClosedChildBoxes, _obj.Info.Actions.FindClosedChildBoxes);
      
      if (!_obj.DeadlineInDays.HasValue && !_obj.DeadlineInHours.HasValue && _obj.Routing != Sungero.ExchangeCore.BoxBase.Routing.NoAssignments)
      {
        e.AddError(_obj.Info.Properties.DeadlineInDays, Sungero.ExchangeCore.BoxBases.Resources.NeedSetAssignmentDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.DeadlineInHours });
        e.AddError(_obj.Info.Properties.DeadlineInHours, Sungero.ExchangeCore.BoxBases.Resources.NeedSetAssignmentDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.DeadlineInHours });
      }
      
      if ((_obj.DeadlineInDays ?? 0) + (_obj.DeadlineInHours ?? 0) == 0 && _obj.Routing != Sungero.ExchangeCore.BoxBase.Routing.NoAssignments && e.IsValid)
      {
        e.AddError(_obj.Info.Properties.DeadlineInDays, Sungero.ExchangeCore.BoxBases.Resources.IncorrectHoursAssignmentDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.DeadlineInHours });
        e.AddError(_obj.Info.Properties.DeadlineInHours, Sungero.ExchangeCore.BoxBases.Resources.IncorrectHoursAssignmentDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.DeadlineInHours });
      }
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      base.Saving(e);
      
      // Включение ответственного в роль "Ответственные за сервисы обмена".
      var prevResponsible = _obj.State.Properties.Responsible.OriginalValue;
      if (_obj.State.Properties.Responsible.IsChanged && !Equals(_obj.Responsible, prevResponsible))
      {
        var role = ExchangeCore.Functions.Module.GetExchangeServiceUsersRole();
        if (role != null)
        {
          if (!ExchangeCore.BoxBases.GetAll().Where(x => !Equals(x, _obj)).Any(x => Equals(x.Responsible, prevResponsible)))
          {
            var link = role.RecipientLinks.Where(x => Equals(x.Member, prevResponsible)).FirstOrDefault();
            if (link != null)
              role.RecipientLinks.Remove(link);
          }
          
          if (_obj.Responsible != null && !role.RecipientLinks.Any(x => Equals(x.Member, _obj.Responsible)))
            role.RecipientLinks.AddNew().Member = _obj.Responsible;
        }
      }
    }
  }

}