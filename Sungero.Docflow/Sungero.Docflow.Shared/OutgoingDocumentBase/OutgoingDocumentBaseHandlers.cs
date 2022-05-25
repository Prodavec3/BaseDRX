using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;

namespace Sungero.Docflow
{
  partial class OutgoingDocumentBaseAddresseesSharedCollectionHandlers
  {

    public virtual void AddresseesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = (_obj.Addressees.Max(a => a.Number) ?? 0) + 1;
    }

    public virtual void AddresseesDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      if (_obj.IsManyAddressees == true && _obj.InResponseTo != null && !_obj.Addressees.Any(x => Equals(x.Correspondent, _obj.InResponseTo.Correspondent)))
        _obj.InResponseTo = null;
    }
  }

  partial class OutgoingDocumentBaseAddresseesSharedHandlers
  {

    public virtual void AddresseesAddresseeChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseAddresseesAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && !Equals(e.NewValue.Company, _obj.Correspondent))
        _obj.Correspondent = e.NewValue.Company;
    }
    
    public virtual void AddresseesCorrespondentChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseAddresseesCorrespondentChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
      {
        if (_obj.Addressee != null && !Equals(_obj.Addressee.Company, e.NewValue))
          _obj.Addressee = null;
        
        if (_obj.OutgoingDocumentBase.InResponseTo != null && !_obj.OutgoingDocumentBase.Addressees.Any(x => Equals(x.Correspondent, _obj.OutgoingDocumentBase.InResponseTo.Correspondent)))
          _obj.OutgoingDocumentBase.InResponseTo = null;
      }
    }
  }

  partial class OutgoingDocumentBaseSharedHandlers
  {

    public override void DeliveryMethodChanged(Sungero.Docflow.Shared.OfficialDocumentDeliveryMethodChangedEventArgs e)
    {
      base.DeliveryMethodChanged(e);
      
      if (!Equals(e.NewValue, e.OldValue))
        this.SyncAddressees();
    }

    public virtual void IsManyAddresseesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (_obj.IsManyAddressees == true)
      {
        Functions.OutgoingDocumentBase.ClearAndFillFirstAddressee(_obj);
        
        _obj.Correspondent = Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty();
        _obj.DistributionCorrespondent = _obj.Correspondent.Name;
        _obj.DeliveryMethod = null;
        _obj.Addressee = null;
      }
      else if (_obj.IsManyAddressees == false)
      {
        var addressee = _obj.Addressees.OrderBy(a => a.Number).FirstOrDefault(a => a.Correspondent != null);
        if (addressee != null)
        {
          _obj.Correspondent = addressee.Correspondent;
          _obj.DeliveryMethod = addressee.DeliveryMethod;
          _obj.Addressee = addressee.Addressee;
        }
        else
        {
          _obj.Correspondent = null;
          _obj.DeliveryMethod = null;
          _obj.Addressee = null;
        }
        
        Functions.OutgoingDocumentBase.ClearAndFillFirstAddressee(_obj);
      }
    }

    public virtual void InResponseToChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseInResponseToChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      _obj.Relations.AddFromOrUpdate(Constants.Module.ResponseRelationName, e.OldValue, e.NewValue);

      if (e.NewValue == null)
        return;

      if (_obj.IsManyAddressees == false && !Equals(_obj.Correspondent, e.NewValue.Correspondent))
        _obj.Correspondent = e.NewValue.Correspondent;
      
      if (_obj.IsManyAddressees == true && !_obj.Addressees.Any())
      {
        var newAddressee = _obj.Addressees.AddNew();
        newAddressee.Correspondent = e.NewValue.Correspondent;
      }
      
      Functions.OfficialDocument.CopyProjects(e.NewValue, _obj);
    }

    public virtual void AddresseeChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && !Equals(e.NewValue.Company, _obj.Correspondent))
        _obj.Correspondent = e.NewValue.Company;
      
      if (!Equals(e.NewValue, e.OldValue))
        this.SyncAddressees();
    }

    public virtual void CorrespondentChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseCorrespondentChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
      {
        _obj.State.Properties.Addressee.IsEnabled = Sungero.Parties.CompanyBases.Is(e.NewValue) || e.NewValue == null;
        if (!_obj.State.Properties.Addressee.IsEnabled ||
            (_obj.Addressee != null && !Equals(_obj.Addressee.Company, e.NewValue)))
          _obj.Addressee = null;
        
        if (_obj.IsManyAddressees == false && _obj.InResponseTo != null && _obj.InResponseTo.Correspondent != _obj.Correspondent)
          _obj.InResponseTo = null;
      }
      
      if (!Equals(e.NewValue, e.OldValue))
        this.SyncAddressees();
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.SubjectChanged(e);
    }
    
    private void SyncAddressees()
    {
      if (_obj.IsManyAddressees == true)
      {
        _obj.Correspondent = Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty();
        _obj.DeliveryMethod = null;
        _obj.Addressee = null;
      }
      else if (_obj.IsManyAddressees == false)
        Functions.OutgoingDocumentBase.ClearAndFillFirstAddressee(_obj);
    }
  }
}
