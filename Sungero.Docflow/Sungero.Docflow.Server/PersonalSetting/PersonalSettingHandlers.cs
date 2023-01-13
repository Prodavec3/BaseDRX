using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PersonalSetting;

namespace Sungero.Docflow
{
  partial class PersonalSettingResolutionAuthorPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResolutionAuthorFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return Functions.Module.UsersCanBeResolutionAuthor(null).Cast<T>().AsQueryable();
    }
  }

  partial class PersonalSettingContractDocRegisterPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ContractDocRegisterFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(l => Recipients.OwnRecipientIdsFor(_obj.Employee).Contains(l.RegistrationGroup.Id) && l.DocumentFlow == DocumentRegister.DocumentFlow.Contracts);
    }
  }

  partial class PersonalSettingInnerDocRegisterPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> InnerDocRegisterFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(l => Recipients.OwnRecipientIdsFor(_obj.Employee).Contains(l.RegistrationGroup.Id) && l.DocumentFlow == DocumentRegister.DocumentFlow.Inner);
    }
  }

  partial class PersonalSettingOutgoingDocRegisterPropertyFilteringServerHandler<T>
  {
    
    public virtual IQueryable<T> OutgoingDocRegisterFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query
        .Where(l => l.RegisterType == DocumentRegister.RegisterType.Numbering || Recipients.OwnRecipientIdsFor(_obj.Employee).Contains(l.RegistrationGroup.Id))
        .Where(l => l.DocumentFlow == DocumentRegister.DocumentFlow.Outgoing);
    }
  }

  partial class PersonalSettingIncomingDocRegisterPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> IncomingDocRegisterFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(l => Recipients.OwnRecipientIdsFor(_obj.Employee).Contains(l.RegistrationGroup.Id) && l.DocumentFlow == DocumentRegister.DocumentFlow.Incoming);
    }
  }

  partial class PersonalSettingServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (PersonalSettings.GetAll(s => Equals(s.Employee, _obj.Employee) && !Equals(s, _obj)).Any())
        throw AppliedCodeException.Create(PersonalSettings.Resources.CantCreateMoreSetting);
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.IsAutoCalcSupervisor = true;
      _obj.IsAutoCalcResolutionAuthor = true;
      _obj.FollowUpActionItem = false;
      _obj.IsAutoExecLeadingActionItem = false;
      _obj.Period = null;
      _obj.ShowRegPane = true;
      _obj.MyContractsNotification = true;
      _obj.MySubordinatesContractsNotification = true;
      _obj.PrintSender = true;
      _obj.ShowNotApproveSign = false;
      _obj.MyPowersOfAttorneyNotification = true;
      _obj.MySubordinatesPowersOfAttorneyNotification = true;
      _obj.RegistrationStampPosition = RegistrationStampPosition.BottomRight;
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      throw AppliedCodeException.Create(PersonalSettings.Resources.CantDeleteSetting);
    }
  }

}