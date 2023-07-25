autopress := False

SetTimer teleport, 250
return

F::
{
	autopress := Not(autopress)
	Return
}

teleport:
{
	if (autopress)
	{
		Send, {x}
	}
	Return
}

F10::Reload