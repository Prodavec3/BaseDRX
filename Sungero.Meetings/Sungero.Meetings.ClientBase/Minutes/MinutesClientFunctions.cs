using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Minutes;
using Sungero.RecordManagement.ActionItemExecutionTask;

namespace Sungero.Meetings.Client
{
  partial class MinutesFunctions
  {
    public override void CreateActionItemsFromDocumentDialog(Sungero.Core.IValidationArgs e)
    {
      // Не доступно, если нет лицензии на модуль Совещания.
      var moduleGuid = Sungero.Meetings.Constants.Module.MeetingsUIGuid;
      if (!Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(moduleGuid))
      {
        Dialogs.NotifyMessage(Minuteses.Resources.ActionItemCreationDialogNoMeetingsLicense);
        return;
      }
      
      base.CreateActionItemsFromDocumentDialog(e);
    }
  }
}