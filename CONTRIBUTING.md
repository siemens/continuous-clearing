# Contributing

When contributing to this repository, first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change. Make sure a pull request is made after every changes before merging to master.

Note we have a code of conduct, follow it in all your interactions with the project

We welcome contributions in several forms, e.g.

- Documenting
- Testing
- Coding


## Getting Started
1. Installation process - 
    Install the following basic prerequisites:
    * Git (any recent version will do) -
      Clone the repository from <CA_Project_RepoLink>

2. Software dependencies -
     Visual Studio 2022, .NET 8
	
## Pull Request Process

1. Ensure any installed or build dependencies are removed before the end of the layer when doing a build.
2. Update the README.md and Changelog.md with details of changes to the code, this includes new environment variables, exposed ports, useful file locations and container parameters.
3. Ensure the PR description clearly describes the problem and solution.

## Running the tests
The methods in all the the 3 executables are tested through Unit Tests (UTs). 
Workflows are validated through Integration Tests (IT).
After making any code changes ensure that proper UT and IT test cases are also added.

The simplest way to run tests:
```bash
- cd src
- dotnet test --no-build -c release
 ```



