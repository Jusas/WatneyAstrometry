# Benchmarking setup

This is a program that reads a config file, and then performs benchmark runs
accordingly.

Downloading of the data files is required first.
_Or technically not, you can manually set up your data directory if you like_.

- Run `quad_dbs/*/download.sh` to download the quad databases. They will be downloaded
  to the same directory where the script sits, from GitHub releases.
- Run `benchmark_images/download.sh` to download the benchmark image package, from GitHub releases.
  It will be extracted to the same directory where the script sits.
- Run `watney_binaries/*/download.sh` to download different release versions of Watney CLI, 
  from GitHub releases. It will be extracted to the same directory where the script sits.

Once all data is downloaded which is required by the benchmarking runs, the program can be run.
The program will read the `config.json` that is given to it as the first argument, and for each
Watney solver listed in it, it will run solve benchmarks using the image files listed in the config.
The solver will be run for each combination of the variations listed in the config file.
The output will be a CSV file per each solver + image combo, containing the results of all the setting variations
found in the config.

## Example usage

```shell
cd bin/Release/net9.0

# First argument: config JSON file
# Second argument: data root directory 
./watney-bench ../../../config.json ../../../
```

## Config JSON

Note: Currently only contains the benchmarking setup for blind solves.

The paths in the config file are relative to the __root data directory__ that is given
as the second argument to the executable.

- `blind.watney_solvers.prefix`: Prefix used in result file names
- `blind.watney_solvers.name`: Name printed in the CSV and output
- `blind.watney_solvers.dir`: Directory where the watney solver is installed
- `blind.watney_solvers.config_file`: Solver config file
- `blind.files`: A list of files to include in the benchmark, can use wildcards
- `blind.output_dir`: Directory where results are saved
- `blind.sampling_variations`: List of sampling values to use in the benchmark runs
- `blind.radius_variations`: List of \[min radius, max radius\] combinations to use in the benchmark runs
- `blind.offset_variations`: List of \[lower density offset, higher density offset\] combinations to use in the benchmark runs

