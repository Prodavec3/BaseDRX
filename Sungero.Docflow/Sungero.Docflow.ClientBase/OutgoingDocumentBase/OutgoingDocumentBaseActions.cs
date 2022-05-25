using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;
using Sungero.Reporting;

namespace Sungero.Docflow.Client
{
  partial class OutgoingDocumentBaseAnyChildEntityCollectionActions
  {
    public override void DeleteChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteChildEntity(e);
    }

    public override bool CanDeleteChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      // Дизейбл грида при резервировании номера/регистрации.
      var entity = OutgoingDocumentBases.As(e.RootEntity);
      return entity != null && _all == entity.Addressees && Functions.OutgoingDocumentBase.DisableAddresseesOnRegistration(entity, e)
        ? false
        : base.CanDeleteChildEntity(e);
    }
  }

  partial class OutgoingDocumentBaseAnyChildEntityActions
  {
    public override void CopyChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CopyChildEntity(e);
    }

    public override bool CanCopyChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      // Дизейбл грида при резервировании номера/регистрации.
      var entity = OutgoingDocumentBases.As(e.RootEntity);
      return entity != null && _all == entity.Addressees && Functions.OutgoingDocumentBase.DisableAddresseesOnRegistration(entity, e)
        ? false
        : base.CanCopyChildEntity(e);
    }

    public override void AddChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.AddChildEntity(e);
    }

    public override bool CanAddChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      // Дизейбл грида при резервировании номера/регистрации.
      var entity = OutgoingDocumentBases.As(e.RootEntity);
      return entity != null && _all == entity.Addressees && Functions.OutgoingDocumentBase.DisableAddresseesOnRegistration(entity, e)
        ? false
        : base.CanAddChildEntity(e);
    }
  }

  internal static class OutgoingDocumentBaseAddresseesStaticActions
  {
    public static bool CanFillFromDistributionList(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var obj = OutgoingDocumentBases.As(e.Entity);
      return obj.IsManyAddressees == true && !Functions.OutgoingDocumentBase.DisableAddresseesOnRegistration(obj, e);
    }

    public static void FillFromDistributionList(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var obj = OutgoingDocumentBases.As(e.Entity);
      var distributionLists = Functions.Module.Remote.GetDistributionLists();
      var distributionList = distributionLists.ShowSelect();
      if (distributionList == null)
        return;
      
      foreach (var addressee in distributionList.Addressees.OrderBy(a => a.Number))
      {
        var newAddressee = obj.Addressees.AddNew();
        newAddressee.Correspondent = addressee.Correspondent;
        newAddressee.Addressee = addressee.Addressee;
        newAddressee.DeliveryMethod = addressee.DeliveryMethod;
      }
    }

    public static bool CanSaveToDistributionList(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var obj = OutgoingDocumentBases.As(e.Entity);
      return obj.IsManyAddressees == true && DistributionLists.AccessRights.CanCreate();
    }

    public static void SaveToDistributionList(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var obj = OutgoingDocumentBases.As(e.Entity);
      var distributionList = Functions.Module.Remote.CreateDistributionList();
      foreach (var addressee in obj.Addressees.OrderBy(a => a.Number))
      {
        var newAddressee = distributionList.Addressees.AddNew();
        newAddressee.Correspondent = addressee.Correspondent;
        newAddressee.Addressee = addressee.Addressee;
        newAddressee.DeliveryMethod = addressee.DeliveryMethod; 
      }
      distributionList.Show();
    }

    public static bool CanPrintDistributionSheetChild(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var obj = OutgoingDocumentBases.As(e.Entity);
      return !obj.State.IsInserted && !obj.State.IsChanged && obj.IsManyAddressees == true;
    }

    public static void PrintDistributionSheetChild(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var report = Docflow.Reports.GetDistributionSheetReport();
      report.OutgoingDocument = OutgoingDocumentBases.As(e.Entity);
      report.Open();
    }
  }

  partial class OutgoingDocumentBaseActions
  {
    public virtual void ChangeManyAddressees(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsManyAddressees == false)
        Dialogs.NotifyMessage(OutgoingDocumentBases.Resources.FillDistributionListOnAdressesTab);
      
      if (_obj.IsManyAddressees == true && _obj.Addressees.Count(a => a.Correspondent != null) > 1)
      {
        var addresseeRaw = _obj.Addressees.OrderBy(a => a.Number).FirstOrDefault(a => a.Correspondent != null);
        var addresseeName = string.Empty;
        var correspondentName = addresseeRaw.Correspondent.Name;
        if (addresseeRaw.Addressee != null)
        {
          if (addresseeRaw.Addressee.Person != null)
          {
            var person = addresseeRaw.Addressee.Person;
            addresseeName = Parties.PublicFunctions.Module.GetSurnameAndInitialsInTenantCulture(person.FirstName, person.MiddleName, person.LastName);
          }
          else
          {
            var contactName = CaseConverter.SplitPersonFullName(addresseeRaw.Addressee.Name);
            addresseeName = Parties.PublicFunctions.Module.GetSurnameAndInitialsInTenantCulture(contactName.FirstName, contactName.MiddleName, contactName.LastName);
          }
          addresseeName = string.Format("{0} ({1})", addresseeName, correspondentName);
        }
        else
          addresseeName = correspondentName;
        
        var dialog = Dialogs.CreateTaskDialog(OfficialDocuments.Resources.ChangeManyAddresseesQuestion,
                                              OfficialDocuments.Resources.ChangeManyAddresseesDescriptionFormat(addresseeName), MessageType.Question);
        dialog.Buttons.AddYesNo();
        if (dialog.Show() == DialogButtons.Yes)
          _obj.IsManyAddressees = !_obj.IsManyAddressees;
      }
      else
        _obj.IsManyAddressees = !_obj.IsManyAddressees;
    }

    public virtual bool CanChangeManyAddressees(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      // Смена режима многоадресности доступна только пользователям с правами на изменение документа.
      return _obj.AccessRights.CanUpdate() ? _obj.State.Properties.IsManyAddressees.IsEnabled : false;
    }

    public override void AssignNumber(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsManyAddressees == true && !_obj.Addressees.Any())
      {
        e.AddError(OutgoingDocumentBases.Resources.NeedFillAddressee);
        return;
      }

      base.AssignNumber(e);
    }

    public override bool CanAssignNumber(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanAssignNumber(e);
    }

    public override void Register(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsManyAddressees == true && !_obj.Addressees.Any())
      {
        e.AddError(OutgoingDocumentBases.Resources.NeedFillAddressee);
        return;
      }
      
      base.Register(e);
    }

    public override bool CanRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRegister(e);
    }

    public virtual void PrintDistributionSheet(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsManyAddressees != true)
      {
        Dialogs.NotifyMessage(OutgoingDocumentBases.Resources.SpecifyMultipleRecipients);
        return;
      }
      
      var report = Docflow.Reports.GetDistributionSheetReport();
      report.OutgoingDocument = _obj;
      report.Open();
    }

    public virtual bool CanPrintDistributionSheet(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

    public virtual void PrintEnvelopeCard(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.OutgoingDocumentBase.ShowSelectEnvelopeFormatDialog(new List<IOutgoingDocumentBase>() { _obj }, null);
    }

    public virtual bool CanPrintEnvelopeCard(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }
  }

  partial class OutgoingDocumentBaseCollectionActions
  {
    public virtual bool CanPrintEnvelope(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_objs.Any(t => t.State.IsInserted || t.State.IsChanged);
    }

    public virtual void PrintEnvelope(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.OutgoingDocumentBase.ShowSelectEnvelopeFormatDialog(_objs.ToList(), null);
    }

    public virtual bool CanMailRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void MailRegister(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = Docflow.Reports.GetMailRegisterReport();
      report.OutgoingDocuments.AddRange(_objs);
      report.Open();
    }
  }

  internal static class OutgoingDocumentBaseStaticActions
  {
    public static bool CanDocumentRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return RecordManagement.PublicFunctions.Module.GetOutgoingDocumentsReport().CanExecute();
    }

    public static void DocumentRegister(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      RecordManagement.PublicFunctions.Module.GetOutgoingDocumentsReport().Open();
    }
  }
}