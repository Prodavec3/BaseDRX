using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow.Client
{
  internal static class MemoAddresseesStaticActions
  {
    public static bool CanFillFromAcquaintanceList(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var obj = Memos.As(e.Entity);
      return obj.State.Properties.Addressees.IsEnabled;
    }
    
    public static void FillFromAcquaintanceList(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var obj = Memos.As(e.Entity);
      var acquaintanceLists = RecordManagement.PublicFunctions.Module.Remote.GetAcquaintanceLists();
      var acquaintanceList = acquaintanceLists.ShowSelect();
      var errorMessage = Functions.Memo.TryFillFromAcquaintanceList(obj, acquaintanceList);
      if (!string.IsNullOrWhiteSpace(errorMessage))
        Dialogs.NotifyMessage(errorMessage);
    }

    public static bool CanSaveToAcquaintanceList(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var obj = Memos.As(e.Entity);
      return obj.IsManyAddressees == true;
    }

    public static void SaveToAcquaintanceList(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var obj = Memos.As(e.Entity);
      var acquaintanceList = RecordManagement.PublicFunctions.Module.Remote.CreateAcquaintanceList();
      
      foreach (var addressee in obj.Addressees)
      {
        var newParticipantRow = acquaintanceList.Participants.AddNew();
        newParticipantRow.Participant = addressee.Addressee;
      }
      acquaintanceList.Show();
    }
  }

  partial class MemoActions
  {
    public virtual void ChangeManyAddressees(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsManyAddressees == false)
      {
        Dialogs.NotifyMessage(OfficialDocuments.Resources.FillAddresseesOnAddresseesTab);
        _obj.IsManyAddressees = true;
      }
      else if (_obj.IsManyAddressees == true)
      {
        if (_obj.Addressees.Count(a => a.Addressee != null) > 1)
        {
          var addresseeRaw = _obj.Addressees.OrderBy(a => a.Number).FirstOrDefault(a => a.Addressee != null);
          var addresseeName = addresseeRaw.Addressee.Person.ShortName;
          var dialog = Dialogs.CreateTaskDialog(OfficialDocuments.Resources.ChangeManyAddresseesQuestion,
                                                OfficialDocuments.Resources.ChangeManyAddresseesDescriptionFormat(addresseeName), MessageType.Question);
          dialog.Buttons.AddYesNo();
          if (dialog.Show() == DialogButtons.Yes)
            _obj.IsManyAddressees = false;
        }
        else
        {
          _obj.IsManyAddressees = false;
        }
      }
    }

    public virtual bool CanChangeManyAddressees(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      // Смена режима многоадресности доступна только пользователям с правами на изменение документа.
      return _obj.AccessRights.CanUpdate() ? _obj.State.Properties.IsManyAddressees.IsEnabled : false;
    }
  }
}