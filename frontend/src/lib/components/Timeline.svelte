<script lang="ts">
	const timeFormatter = new Intl.DateTimeFormat(undefined, {
		hour: '2-digit',
		minute: '2-digit',
		hour12: false
	});

	// eslint-disable-next-line svelte/prefer-svelte-reactivity
	const dateTimeToSet = new Date('25 Jan 2026');

	function formatShortTime(hour: number): string {
		return timeFormatter.format(dateTimeToSet.setHours(hour, 0, 0, 0));
	}
</script>

<div class="w-8 flex-initial text-center text-xs">
	<div class="h-8 align-middle">
		<p class="relative top-4">{formatShortTime(8)}</p>
	</div>
	{#each { length: 12 }, i}
		<div class="h-12 align-middle">
			<p class="relative top-8">{formatShortTime(i + 9)}</p>
		</div>
	{/each}
</div>
