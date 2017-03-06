REM Labyrinth - The abbey library from "The Name of the Rose" - In VB.NET - May 3, 2013

' is the same as REM

Module MainModule
    REM Functions in a Module cannot be declared as Shared

    Public Sub Main(ByVal args As String())

        Try
            ' The third command-line parameter indicates how many extra connections should be added to the labyrinth
            ' once it is a single blob.  This will introduce cycles, which should make the labyrinth more challenging to navigate.
            Dim numberOfExtraConnections As Integer = 0

            If args.Length < 2 Or args.Length > 3 Then
                Console.WriteLine("Usage: Labyrinth.exe (NumberOfLevels) (NumberOfRoomsPerLevel) [NumberOfExtraConnections]")
                Return
            End If

            If args.Length > 2 Then
                numberOfExtraConnections = Integer.Parse(args(2))
            End If

            Dim generator As LabyrinthGenerator = New LabyrinthGenerator(Integer.Parse(args(0)), Integer.Parse(args(1)), numberOfExtraConnections)

            generator.Generate()
            generator.Report()
            generator.PrintLongestPath()
            generator.NavigateLabyrinth()
        Catch ex As Exception
            Console.WriteLine("Caught {0}: {1}", ex.GetType().FullName, ex.Message)
            Console.WriteLine("Stack trace:")
            Console.WriteLine(ex.StackTrace)
        End Try
    End Sub

End Module
