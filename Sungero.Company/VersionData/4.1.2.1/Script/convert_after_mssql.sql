-- Если свойство Нерезидент не определено, сделать НОР по умолчанию резидентом.
update Sungero_Core_Recipient
set Nonresident_Company_Sungero = 0
where Discriminator = 'eff95720-181f-4f7d-892d-dec034c7b2ab'
  and Nonresident_Company_Sungero is null