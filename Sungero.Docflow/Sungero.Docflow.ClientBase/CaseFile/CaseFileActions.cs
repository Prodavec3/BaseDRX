using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CaseFile;

namespace Sungero.Docflow.Client
{

  partial class CaseFileActions
  {

    public virtual void ShowDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var documents = Functions.CaseFile.Remote.ShowCaseDocuments(_obj);
      documents.Show(CaseFiles.Resources.DocumentsOfFileFormat(_obj.DisplayValue));
    }

    public virtual bool CanShowDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }
  }
}