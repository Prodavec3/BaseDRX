do $$
begin
  
if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_approvalrule' and column_name = 'needrestrinar')
then
-- Выключить Ограничить права инициатора на документ и приложения.
update
  sungero_docflow_approvalrule
set
  needrestrinar = false
where 
  needrestrinar is null;
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_approvalstage' and column_name = 'needrestrperar') 
then
-- Выключить Ограничить права исполнителя после выполнения задания.
update
  sungero_docflow_approvalstage
set
  needrestrperar = false
where 
  needrestrperar is null;
end if;
  
if exists(select *
          from information_schema.columns
          where table_name = 'sungero_docflow_approvalstage' and column_name = 'rightstype') 
then
-- Установить тип прав по умолчанию.
update
  sungero_docflow_approvalstage
set
  rightstype = case when stagetype = 'Print' then 'Read' else 'Edit' end
where 
  rightstype is null;
end if;

end $$;