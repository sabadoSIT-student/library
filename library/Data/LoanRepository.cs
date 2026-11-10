
using library.Models;
using Npgsql;

namespace library.Data;

public class LoanRepository
{
    public const int MaxOpenLoans = 3;

    private readonly NpgsqlDataSource _dataSource;

    public LoanRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    private static LoanView ReadLoan(NpgsqlDataReader reader)
    {
        return new LoanView
        {
            LoanId = reader.GetInt64(0),
            MemberId = reader.GetInt64(1),
            MemberName = reader.GetString(2),
            BookTitle = reader.GetString(3),
            LoanedOn = reader.GetFieldValue<DateOnly>(4),
            ReturnedOn = reader.IsDBNull(5)
                ? null
                : reader.GetFieldValue<DateOnly>(5),
            DaysAllowed = reader.GetInt32(6),
            Status = reader.GetString(7)
        };
    }

    public async Task<List<LoanView>> GetAllAsync()
    {
        const string sql = @"
            SELECT
                l.loan_id,
                l.member_id,
                m.full_name,
                b.title,
                l.loaned_on,
                l.returned_on,
                l.days_allowed,
                CASE
                    WHEN l.returned_on IS NOT NULL THEN 'Returned'
                    WHEN CURRENT_DATE > l.loaned_on + l.days_allowed THEN 'Overdue'
                    ELSE 'Out'
                END AS status
            FROM lending.loan l
            JOIN lending.member m
                ON m.member_id = l.member_id
            JOIN lending.book b
                ON b.book_id = l.book_id
            ORDER BY l.loaned_on DESC;";

        var loans = new List<LoanView>();

        await using var cmd = _dataSource.CreateCommand(sql);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(ReadLoan(reader));
        }

        return loans;
    }

    // BORROW: lend one book, unless the member already has MaxOpenLoans books out.
    public async Task<bool> BorrowAsync(long memberId, long bookId, int daysAllowed)
    {
        // 1. Open one connection.
        await using var connection = await _dataSource.OpenConnectionAsync();

        // 2. Start a transaction.
        await using var transaction = await connection.BeginTransactionAsync();

        // 3. Lock this member's row.
        await using (var lockMember = new NpgsqlCommand(
            "SELECT member_id FROM lending.member WHERE member_id = @member_id FOR UPDATE;",
            connection, transaction))
        {
            lockMember.Parameters.AddWithValue("member_id", memberId);
            await lockMember.ExecuteNonQueryAsync();
        }

        // 4. Count the member's open loans.
        long openLoans;

        await using (var count = new NpgsqlCommand(
            "SELECT COUNT(*) FROM lending.loan WHERE member_id = @member_id AND returned_on IS NULL;",
            connection, transaction))
        {
            count.Parameters.AddWithValue("member_id", memberId);
            openLoans = Convert.ToInt64(await count.ExecuteScalarAsync());
        }

        // 5. Check if the member reached the limit.
        if (openLoans >= MaxOpenLoans)
        {
            // 6. Undo the transaction.
            await transaction.RollbackAsync();

            // 7. Nothing was borrowed.
            return false;
        }

        // 8. Record the loan.
        await using (var insert = new NpgsqlCommand(
            "INSERT INTO lending.loan (member_id, book_id, days_allowed) VALUES (@member_id, @book_id, @days_allowed);",
            connection, transaction))
        {
            insert.Parameters.AddWithValue("member_id", memberId);
            insert.Parameters.AddWithValue("book_id", bookId);
            insert.Parameters.AddWithValue("days_allowed", daysAllowed);
            await insert.ExecuteNonQueryAsync();
        }

        // 9. Commit the transaction.
        await transaction.CommitAsync();

        // 10. Report success.
        return true;
    }
}
