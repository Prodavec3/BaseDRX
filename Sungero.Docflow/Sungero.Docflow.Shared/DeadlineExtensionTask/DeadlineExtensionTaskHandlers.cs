using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;

namespace Sungero.Docflow
{
  partial class DeadlineExtensionTaskSharedHandlers
  {

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > DeadlineExtensionTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, DeadlineExtensionTasks.Info.Properties.Subject.Length);
    }

  }
}