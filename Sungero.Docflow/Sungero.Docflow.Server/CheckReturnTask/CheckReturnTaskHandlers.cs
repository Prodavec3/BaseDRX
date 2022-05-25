using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CheckReturnTask;

namespace Sungero.Docflow
{
  partial class CheckReturnTaskServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      // Получить ресурсы в культуре тенанта.
      using (TenantInfo.Culture.SwitchTo())
        _obj.ActiveText = CheckReturnTasks.Resources.TaskActiveText;
      _obj.NeedsReview = false;
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj, e);
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        var subject = string.Empty;
        using (TenantInfo.Culture.SwitchTo())
          subject = CheckReturnTasks.Resources.ReturnTaskSubjectFormat(document.Name);
        if (subject != _obj.Subject)
          _obj.Subject = subject;
        
        if (document.DocumentRegister != null && document.DocumentRegister.RegistrationGroup != null)
        {
          IGroup registrationGroup = document.DocumentRegister.RegistrationGroup;
          _obj.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
        }
      }
      
      // Выдать права на документы для всех, кому выданы права на задачу.
      if (_obj.State.IsChanged)
        Functions.Module.GrantManualReadRightForAttachments(_obj, _obj.AllAttachments.ToList());
    }
  }
}