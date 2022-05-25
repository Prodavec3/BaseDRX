using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentKind;

namespace Sungero.Docflow
{
  partial class DocumentKindClientHandlers
  {

    public virtual void CodeValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(e.NewValue) || e.NewValue == e.OldValue)
        return;
      
      // Использование пробелов в середине кода запрещено.
      var newCode = e.NewValue.Trim();
      if (Regex.IsMatch(newCode, @"\s"))
        e.AddError(Company.Resources.NoSpacesInCode);
    }

    public virtual void ProjectsAccountingValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      _obj.State.Properties.GrantRightsToProject.IsEnabled = e.NewValue == true;
    }

    public virtual void DocumentTypeValueInput(Sungero.Docflow.Client.DocumentKindDocumentTypeValueInputEventArgs e)
    {
      _obj.ProjectsAccounting = e.NewValue != null && e.NewValue.DocumentTypeGuid == Constants.Module.ProjectDocumentTypeGuid;
      _obj.State.Properties.ProjectsAccounting.IsEnabled = e.NewValue == null || e.NewValue.DocumentTypeGuid != Constants.Module.ProjectDocumentTypeGuid;
      _obj.State.Properties.GrantRightsToProject.IsEnabled = _obj.ProjectsAccounting.Value;
    }
    
    public virtual IEnumerable<Enumeration> NumberingTypeFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.DocumentType != null && _obj.DocumentType.IsRegistrationAllowed != true)
      {
        query = query.Where(t => t == NumberingType.NotNumerable);
      }
      return query;
    }

    public virtual void DeadlineInHoursValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 1)
        e.AddError(DocumentKinds.Resources.IncorrectDeadline);
    }

    public virtual void DeadlineInDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 1)
        e.AddError(DocumentKinds.Resources.IncorrectDeadline);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.DocumentFlow.IsEnabled = _obj.State.IsInserted;
      _obj.State.Properties.AutoNumbering.IsVisible = _obj.NumberingType == NumberingType.Numerable;
      _obj.State.Properties.ProjectsAccounting.IsEnabled = _obj.DocumentType == null || _obj.DocumentType.DocumentTypeGuid != Constants.Module.ProjectDocumentTypeGuid;
      _obj.State.Properties.GrantRightsToProject.IsEnabled = _obj.ProjectsAccounting == true;
    }
  }
}