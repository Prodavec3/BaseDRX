if exists(select *
          from information_schema.columns
          where table_name = 'Sungero_Core_Recipient' and column_name = 'NeedNotifySumA_Company_Sungero')
-- Выключить ежедневную отправку сводки по задачам и заданиям.
update 
  Sungero_Core_Recipient
set 
  NeedNotifySumA_Company_Sungero = case when NeedNotifyExpi_Company_Sungero = 1 or NeedNotifyNewA_Company_Sungero = 1 then 1 else 0 end
where 
  NeedNotifySumA_Company_Sungero is null
  and Discriminator = 'b7905516-2be5-4931-961c-cb38d5677565'