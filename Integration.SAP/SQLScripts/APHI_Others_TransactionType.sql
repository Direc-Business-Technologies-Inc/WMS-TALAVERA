SELECT 
	  T0.Code
	, T0.Name
	, T1.AcctCode
	, T1.AcctName
FROM [@TRANSACTION_TYPE] T0
LEFT JOIN OACT T1 ON T1.FormatCode = T0.U_AcctCode
ORDER BY Code