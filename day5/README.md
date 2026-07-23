# Day 5 – Performance & Load Testing

This folder contains all artefacts related to performance evaluation, load and stress testing, and optimisation of the Employee Management Microservices solution.

## Directory Structure

```
day5/
├─ README.md                     # This overview (you are reading it)
├─ load_testing/
│   └─ k6_test.js                # Sample k6 load‑test script (placeholder)
├─ stress_testing/
│   └─ k6_stress_test.js         # Sample k6 stress‑test script (placeholder)
├─ scripts/
│   └─ run_load_tests.ps1        # PowerShell helper to execute k6 tests
├─ reports/
│   └─ performance_report.md     # Template for the performance report
├─ config/
│   └─ scaling.yaml              # Azure Container Apps scaling configuration (placeholder)
└─ docs/
    └─ explanations.md           # Explanations of key concepts (load testing, stress testing, etc.)
```

## How to Use

- **Load Testing**: Edit `load_testing/k6_test.js` with the desired scenarios and run `scripts/run_load_tests.ps1`.
- **Stress Testing**: Edit `stress_testing/k6_stress_test.js` and run the same PowerShell helper.
- **Scaling**: Adjust `config/scaling.yaml` and apply it to Azure Container Apps.
- **Reporting**: Fill in `reports/performance_report.md` with the results, bottlenecks, and optimisation steps.

The scripts are placeholders; you can replace them with your preferred load‑testing framework (e.g., k6, Artillery, JMeter). The PowerShell helper assumes `k6` is installed and available on the PATH.
