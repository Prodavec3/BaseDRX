do $$
begin
if exists(select *
          from information_schema.columns
          where table_name = 'sungero_core_recipient' and column_name = 'neednotifysuma_company_sungero')
then
-- Выключить ежедневную отправку сводки по задачам и заданиям.
update
  sungero_core_recipient
set
  neednotifysuma_company_sungero = case when neednotifyexpi_company_sungero = true or neednotifynewa_company_sungero = true then true else false end
where 
  neednotifysuma_company_sungero is null
  and discriminator = 'b7905516-2be5-4931-961c-cb38d5677565';
end if;
end $$;