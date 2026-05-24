<script lang="ts">
	const timeFormatter = new Intl.DateTimeFormat(undefined, {
		hour: '2-digit',
		minute: '2-digit'
	});

	// eslint-disable-next-line svelte/prefer-svelte-reactivity
	const dateTimeToSet = new Date('25 Jan 2026');

	function formatShortTime(hour: number): string {
		return timeFormatter.format(dateTimeToSet.setHours(hour, 0, 0, 0));
	}
</script>

<div class="row-auto w-24 flex-initial">
	<div class="h-16"></div>
	{#each { length: 13 }, i}
		<div class="h-16 text-center align-middle">
			<p class="relative bottom-8">{formatShortTime(i + 8)}</p>
		</div>
	{/each}
</div>
