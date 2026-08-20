// Mirrors the record DTOs exposed by each AtlasBank service. Field order matches the
// C# records so numeric enum values (see note below) line up positionally.

// --- Enum wire formats -----------------------------------------------------
// AccountService, TransactionService, and CustomerService don't register a
// JsonStringEnumConverter, so System.Text.Json serializes their enums as raw
// integers (declaration order, 0-based). CardService and StatementService do
// register one, so card/statement enums arrive as strings. The maps below
// exist to translate the numeric ones back into readable labels/unions.

export const AccountType = { Checking: 0, Savings: 1 } as const;
export type AccountTypeValue = (typeof AccountType)[keyof typeof AccountType];
export const AccountTypeLabel: Record<AccountTypeValue, string> = {
  0: 'Checking',
  1: 'Savings',
};

export const AccountStatus = { Active: 0, Frozen: 1, Closed: 2 } as const;
export type AccountStatusValue = (typeof AccountStatus)[keyof typeof AccountStatus];
export const AccountStatusLabel: Record<AccountStatusValue, string> = {
  0: 'Active',
  1: 'Frozen',
  2: 'Closed',
};

export const TransactionType = { Deposit: 0, Withdrawal: 1, Transfer: 2 } as const;
export type TransactionTypeValue = (typeof TransactionType)[keyof typeof TransactionType];
export const TransactionTypeLabel: Record<TransactionTypeValue, string> = {
  0: 'Deposit',
  1: 'Withdrawal',
  2: 'Transfer',
};

export const TransactionStatus = { Pending: 0, Completed: 1, Failed: 2 } as const;
export type TransactionStatusValue = (typeof TransactionStatus)[keyof typeof TransactionStatus];
export const TransactionStatusLabel: Record<TransactionStatusValue, string> = {
  0: 'Pending',
  1: 'Completed',
  2: 'Failed',
};

export const CustomerStatus = { Active: 0, Suspended: 1, Closed: 2 } as const;
export type CustomerStatusValue = (typeof CustomerStatus)[keyof typeof CustomerStatus];
export const CustomerStatusLabel: Record<CustomerStatusValue, string> = {
  0: 'Active',
  1: 'Suspended',
  2: 'Closed',
};

// Card & statement enums travel as strings already.
export type CardType = 'Debit' | 'Credit';
export type CardStatus = 'Active' | 'Frozen' | 'Expired' | 'Cancelled';

// --- Customers ---------------------------------------------------------

export interface Address {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
}

export interface RegisterCustomerRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber: string;
  dateOfBirth: string; // yyyy-MM-dd
  address: Address;
}

export interface UpdateCustomerRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  address: Address;
}

export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  address: Address;
  status: CustomerStatusValue;
  createdAt: string;
}

// --- Accounts ------------------------------------------------------------

export interface CreateAccountRequest {
  type: AccountTypeValue;
  currency?: string;
}

export interface Account {
  id: string;
  customerId: string;
  accountNumber: string;
  type: AccountTypeValue;
  status: AccountStatusValue;
  balance: number;
  currency: string;
  createdAt: string;
}

// --- Transactions ----------------------------------------------------------

export interface DepositRequest {
  accountId: string;
  amount: number;
  description?: string;
}

export interface WithdrawRequest {
  accountId: string;
  amount: number;
  description?: string;
}

export interface TransferRequest {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  description?: string;
}

export interface Transaction {
  id: string;
  accountId: string;
  toAccountId: string | null;
  type: TransactionTypeValue;
  status: TransactionStatusValue;
  amount: number;
  currency: string;
  reference: string;
  description: string | null;
  failureReason: string | null;
  createdAt: string;
  completedAt: string | null;
}

// --- Cards -----------------------------------------------------------------

export interface IssueCardRequest {
  accountId: string;
  type: CardType;
  spendingLimit: number;
}

export interface UpdateSpendingLimitRequest {
  spendingLimit: number;
}

export interface Card {
  id: string;
  accountId: string;
  customerId: string;
  maskedCardNumber: string;
  cardHolderName: string;
  type: CardType;
  status: CardStatus;
  spendingLimit: number;
  expiryDate: string;
  createdAt: string;
  updatedAt: string | null;
}

// --- Statements --------------------------------------------------------

export interface GenerateStatementRequest {
  accountId: string;
  periodStart: string;
  periodEnd: string;
}

export interface StatementLine {
  transactionId: string;
  date: string;
  reference: string;
  description: string;
  type: string;
  amount: number;
  runningBalance: number;
}

export interface Statement {
  id: string;
  accountId: string;
  customerId: string;
  accountNumber: string;
  customerName: string;
  currency: string;
  periodStart: string;
  periodEnd: string;
  openingBalance: number;
  closingBalance: number;
  totalCredits: number;
  totalDebits: number;
  generatedAt: string;
  lines: StatementLine[];
}

export interface StatementSummary {
  id: string;
  accountId: string;
  accountNumber: string;
  periodStart: string;
  periodEnd: string;
  closingBalance: number;
  generatedAt: string;
}
