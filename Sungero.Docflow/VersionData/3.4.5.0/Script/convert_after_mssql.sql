if exists(select *
          from information_schema.columns
          where table_name = 'sungero_content_edoc' and column_name = 'status_docflow_sungero')

update
  Sungero_Content_EDoc
set
  Status_Docflow_Sungero = 'Active'
where
  Status_Docflow_Sungero is null
  and Discriminator = '9abcf1b7-f630-4a82-9912-7f79378ab199'
  
-- 3.3.47.0: Установить в персональных настройках значения по умолчанию для уведомлений по доверенностям
update Sungero_Docflow_PersonSetting
set PofAttoNotif = 1,
        SubPofAttNotif = 1
where PofAttoNotif is null and
      SubPofAttNotif is null

update doc
set doc.Storage_Docflow_Sungero = (select top 1 v.Body_Storage
                                   from Sungero_Content_EDocVersion v
                                   where v.EDoc = doc.Id 
                                   order by Id desc)
from Sungero_Content_EDoc doc
where doc.Storage_Docflow_Sungero is null
and doc.HasVersions = 1
and doc.DocumentKind_Docflow_Sungero is not null