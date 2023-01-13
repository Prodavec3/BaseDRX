using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;

namespace Sungero.RecordManagement.Client
{
  partial class AcquaintanceTaskActions
  {
    public virtual void ExcludeFromAcquaintance(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.AcquaintanceTask.Remote.AllAcquaintanceAssignmentsCreated(_obj))
      {
        Dialogs.ShowMessage(Sungero.RecordManagement.AcquaintanceTasks.Resources.AcquaintanceInCreatingProcess, MessageType.Warning);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(AcquaintanceTasks.Resources.ExcludeFromAcquaintanceDialogTitle);
      dialog.HelpCode = Constants.AcquaintanceTask.ExcludeFromAcquaintanceHelpCode;
      var selectedPerformersText = dialog.AddMultilineString(AcquaintanceTasks.Resources.ExcludeFromAcquaintanceDialogPerformers, false, string.Empty)
        .RowsCount(3);
      var selectPerformersLink = dialog.AddHyperlink(AcquaintanceTasks.Resources.ExcludeFromAcquaintanceDialogExcludePerformers);
      var exсludeButton = dialog.Buttons.AddCustom(AcquaintanceTasks.Resources.ExcludeFromAcquaintanceDialogExcludeButton);
      selectedPerformersText.IsEnabled = false;
      exсludeButton.IsEnabled = false;
      dialog.Buttons.AddCancel();
      
      var assignmentsToAbort = new List<IAcquaintanceAssignment>();
      
      // Выбрать исполнителей, которых нужно исключить.
      selectPerformersLink.SetOnExecute(
        () =>
        {
          var performers = Functions.AcquaintanceTask.Remote.GetAcquaintancePerformers(_obj).ToList();
          var employeesToExclude = performers.ShowSelectMany(AcquaintanceTasks.Resources.ExcludeFromAcquaintanceDialogSelectPerformers);
          if (employeesToExclude.Any())
          {
            selectedPerformersText.Value = string.Join("; ", employeesToExclude.Select(x => x.Person.ShortName));
            assignmentsToAbort = Functions.AcquaintanceTask.Remote.GetAcquaintanceAssignments(_obj, employeesToExclude.ToList());
            exсludeButton.IsEnabled = true;
          }
        });
      
      if (dialog.Show() == exсludeButton)
      {
        // Создать и запустить асинхронный обработчик исключения участников из ознакомления.
        var excludeFromAcquaintanceHandler = RecordManagement.AsyncHandlers.ExcludeFromAcquaintance.Create();
        excludeFromAcquaintanceHandler.AssignmentIds = string.Join(",", assignmentsToAbort.Select(x => x.Id).ToList());
        var completedNotificationText = AcquaintanceTasks.Resources.ExcludeFromAcquaintanceCompletedNotificationFormat(Hyperlinks.Get(_obj.DocumentGroup.All.FirstOrDefault()));
        excludeFromAcquaintanceHandler.ExecuteAsync(completedNotificationText);
      }
    }

    public virtual bool CanExcludeFromAcquaintance(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() &&
        _obj.Status == Sungero.Workflow.Task.Status.InProcess &&
        ClientApplication.ApplicationType != ApplicationType.Desktop &&
        Functions.AcquaintanceTask.HasDocumentAndCanRead(_obj);
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Sungero.RecordManagement.Functions.AcquaintanceTask.ValidateAcquaintanceTaskStart(_obj, e))
        return;
      
      // Замена стандартного диалога подтверждения выполнения действия.
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                 Constants.AcquaintanceTask.StartConfirmDialogID))
        return;
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

    public virtual void ShowNotAutomatedEmployees(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var notAutomatedParticipants = Functions.AcquaintanceTask.Remote.GetNotAutomatedParticipants(_obj);
      notAutomatedParticipants.Show();
    }

    public virtual bool CanShowNotAutomatedEmployees(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowAcquaintanceFormReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!_obj.DocumentGroup.OfficialDocuments.Any())
      {
        e.AddError(AcquaintanceTasks.Resources.DocumentCantBeEmpty);
        return;
      }
      RecordManagement.Functions.Module.GetAcquaintanceFormReport(_obj).Open();
    }

    public virtual bool CanShowAcquaintanceFormReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged && Functions.AcquaintanceTask.HasDocumentAndCanRead(_obj);
    }

    public virtual void FillFromAcquaintanceList(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var acquaintanceLists = Functions.Module.Remote.GetAcquaintanceLists();
      var acquaintanceList = acquaintanceLists.ShowSelect();
      PublicFunctions.AcquaintanceTask.FillFromAcquaintanceList(_obj, acquaintanceList);
    }

    public virtual bool CanFillFromAcquaintanceList(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.Draft;
    }

    public virtual void SaveToAcquaintanceList(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var acquaintanceList = Functions.Module.Remote.CreateAcquaintanceList();
      foreach (var performer in _obj.Performers)
      {
        var newParticipantRow = acquaintanceList.Participants.AddNew();
        newParticipantRow.Participant = performer.Performer;
      }
      foreach (var excludedPerformer in _obj.ExcludedPerformers)
      {
        var newExcludedParticipant = acquaintanceList.ExcludedParticipants.AddNew();
        newExcludedParticipant.ExcludedParticipant = excludedPerformer.ExcludedPerformer;
      }
      acquaintanceList.Show();
    }

    public virtual bool CanSaveToAcquaintanceList(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowAcquaintanceReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!_obj.DocumentGroup.OfficialDocuments.Any())
      {
        e.AddError(AcquaintanceTasks.Resources.DocumentCantBeEmpty);
        return;
      }
      RecordManagement.Functions.Module.GetAcquaintanceReport(_obj).Open();
    }

    public virtual bool CanShowAcquaintanceReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged && Functions.AcquaintanceTask.HasDocumentAndCanRead(_obj);
    }

  }

}