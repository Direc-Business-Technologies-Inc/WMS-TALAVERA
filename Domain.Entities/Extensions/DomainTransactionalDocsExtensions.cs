using Domain.Entities.Transaction.Common;
using Domain.ValueObjects.Transaction;

namespace Domain.Extensions;

public static class DomainTransactionalDocsExtensions
{
    public static T UpdateLsmsDocNums<T>(this T transDoc, AppDocNumVO lsmsDocNums) where T : TransactionalDocumentDEM
    {
        transDoc
            .UpdateAppDocNum(lsmsDocNums);

        return transDoc;
    }
    
    public static T UpdateSapDocNums<T>(this T transDoc, SapDocumentReferenceVO sapDocNums) where T : TransactionalDocumentDEM
    {
        transDoc
            .UpdateSapDocRef(sapDocNums);

        return transDoc;
    }
}
