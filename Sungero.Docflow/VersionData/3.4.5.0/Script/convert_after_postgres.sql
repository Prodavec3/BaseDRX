do $$
begin
if exists(select *
          from information_schema.columns
          where table_name = 'sungero_content_edoc' and column_name = 'status_docflow_sungero')
then
  update 
    Sungero_Content_EDoc
  set 
    Status_Docflow_Sungero = 'Active'
  where 
    Status_Docflow_Sungero is null 
    and Discriminator = '9abcf1b7-f630-4a82-9912-7f79378ab199';
end if;
end $$;

-- 3.3.47.0: Установить в персональных настройках значения по умолчанию для уведомлений по доверенностям
update Sungero_Docflow_PersonSetting
set PofAttoNotif = true,
      SubPofAttNotif = true
where PofAttoNotif is null and
    SubPofAttNotif is null;

update Sungero_Content_EDoc doc
  set Storage_Docflow_Sungero = (select v.Body_Storage
                                   from Sungero_Content_EDocVersion v
                                   where v.EDoc = doc.Id 
                                 order by Id desc
							                   limit 1)
  where doc.Storage_Docflow_Sungero is null
    and doc.HasVersions = TRUE
	and doc.DocumentKind_Docflow_Sungero is not null;