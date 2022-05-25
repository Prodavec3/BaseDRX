using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow.Client
{
  partial class IncomingDocumentBaseFunctions
  {
    
    /// <summary>
    /// Получить текст для отметки документа устаревшим.
    /// </summary>
    /// <returns>Текст для диалога прекращения согласования.</returns>
    public override string GetTextToMarkDocumentAsObsolete()
    {
      return IncomingDocumentBases.Resources.MarkDocumentAsObsolete;
    }
  }
}