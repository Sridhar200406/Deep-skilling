import argparse

def forecast(current: float, growth: float, years: int, compounding_periods_per_year: int = 1) -> float:
    """
    Calculates the future value of an investment based on constant annual growth and compounding frequency.
    
    Args:
        current: The starting principal amount.
        growth: The annual growth rate (e.g., 0.05 for 5%).
        years: The number of periods to forecast.
        compounding_periods_per_year: The number of times interest is compounded per year (e.g., 1 for annually, 12 for monthly).
    """
    if years < 0:
        raise ValueError("The number of years cannot be negative.")
    if current < 0:
        raise ValueError("The starting principal amount cannot be negative.")
    if compounding_periods_per_year <= 0:
        
        raise ValueError("Compounding periods per year must be a positive integer.")

    rate_per_period = growth / compounding_periods_per_year
    total_periods = years * compounding_periods_per_year
    
    return current * (1 + rate_per_period) ** total_periods


def main():
    parser = argparse.ArgumentParser(description="Calculate the future value of an investment.")
    parser.add_argument("--amount", type=float, default=10000.0, help="Starting principal amount (default: 10000.0)")
    parser.add_argument("--growth", type=float, default=0.07, help="Annual growth rate as a decimal (e.g., 0.07 for 7%%) (default: 0.07)")
    parser.add_argument("--years", type=int, default=10, help="Number of years to forecast (default: 10)")
    parser.add_argument("--compounding", type=int, default=1, help="Number of times interest is compounded per year (e.g., 1 for annual, 12 for monthly) (default: 1)")
    args = parser.parse_args()

    try:
        result = forecast(args.amount, args.growth, args.years, args.compounding)
        print(f"Initial Amount: ${args.amount:,.2f}")
        print(f"Annual Growth Rate: {args.growth:.2%}")
        if args.compounding > 1: # Only print compounding frequency if it's not annual
            compounding_terms = {
                12: "monthly",
                4: "quarterly",
                2: "semi-annually",
            }
            compounding_text = compounding_terms.get(
                args.compounding, f"{args.compounding} times per year"
            )
            print(f"Compounding Frequency: {compounding_text}")
        print(f"Forecasted Value after {args.years} years: ${result:,.2f}")
        print(f"Total Interest Earned: ${result - args.amount:,.2f}")
        total_return = (result - args.amount) / args.amount if args.amount > 0 else 0
        print(f"Total Return: {total_return:.2%}")
    except ValueError as e:
        print(f"Configuration Error: {e}")

if __name__ == "__main__":
    main()
