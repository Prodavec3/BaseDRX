-- Если свойство Нерезидент не определено, сделать НОР по умолчанию резидентом.
update sungero_core_recipient
set nonresident_company_sungero = false
where discriminator = 'eff95720-181f-4f7d-892d-dec034c7b2ab'
  and nonresident_company_sungero is null