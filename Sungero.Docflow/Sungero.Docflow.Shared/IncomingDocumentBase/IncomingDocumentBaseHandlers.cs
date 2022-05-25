using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow
{
  partial class IncomingDocumentBaseAddresseesSharedHandlers
  {

    public virtual void AddresseesAddresseeChanged(Sungero.Docflow.Shared.IncomingDocumentBaseAddresseesAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Department != null)
        _obj.Department = e.NewValue.Department;
    }
  }

  partial class IncomingDocumentBaseAddresseesSharedCollectionHandlers
  {

    public virtual void AddresseesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = (_obj.Addressees.Max(a => a.Number) ?? 0) + 1;
    }
  }

  partial class IncomingDocumentBaseSharedHandlers
  {

    public virtual void IsManyAddresseesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
      {
        Functions.IncomingDocumentBase.ClearAndFillFirstAddressee(_obj);
        Functions.IncomingDocumentBase.SetManyAddresseesPlaceholder(_obj);
      }
      else if (e.NewValue == false)
      {
        Functions.IncomingDocumentBase.FillAddresseeFromAddressees(_obj);
        Functions.IncomingDocumentBase.ClearAndFillFirstAddressee(_obj);
      }
    }
    
    public virtual void CorrespondentChanged(Sungero.Docflow.Shared.IncomingDocumentBaseCorrespondentChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && _obj.InResponseTo != null &&
          !_obj.InResponseTo.Addressees.Any(a => Equals(a.Correspondent, _obj.Correspondent)))
        _obj.InResponseTo = null;
    }

    public virtual void InResponseToChanged(Sungero.Docflow.Shared.IncomingDocumentBaseInResponseToChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      _obj.Relations.AddFromOrUpdate(Constants.Module.ResponseRelationName, e.OldValue, e.NewValue);

      if (e.NewValue == null)
        return;
      
      var correspondents = e.NewValue.Addressees.Select(a => a.Correspondent).ToList();
      if (!correspondents.Contains(_obj.Correspondent))
        _obj.Correspondent = e.NewValue.IsManyAddressees.Value ? null : correspondents.FirstOrDefault();

      Functions.OfficialDocument.CopyProjects(e.NewValue, _obj);
    }

    public virtual void AddresseeChanged(Sungero.Docflow.Shared.IncomingDocumentBaseAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && _obj.BusinessUnit == null)
      {
        // Не чистить, если указан адресат с пустой НОР.
        if (e.NewValue.Department.BusinessUnit != null)
          _obj.BusinessUnit = e.NewValue.Department.BusinessUnit;
      }
      
      if (_obj.IsManyAddressees == false)
        Functions.IncomingDocumentBase.ClearAndFillFirstAddressee(_obj);
    }
  }
}