#!/bin/sh
SOLUTIONPATH=$(dirname "$(realpath -s "$0")")
cd $SOLUTIONPATH/HardVacuumRobot
dotnet run
