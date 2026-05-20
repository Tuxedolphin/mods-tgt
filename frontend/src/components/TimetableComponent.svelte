<script lang="ts">
	const heightOfOneHourLessonPx = 16;
</script>

<div class="grid grid-cols-5 grid-rows-12">
	{#each { length: 12 }, y}
		{#each { length: 5 }, x}
			<div
				class="col-start-{x + 1} row-start-{y + 1} h-{heightOfOneHourLessonPx} {y % 2 == 0
					? 'bg-base-300'
					: 'bg-base-200'}"
			></div>
		{/each}
	{/each}

	{#each { length: 5 }, day}
		<div class="relative col-start-{day} row-start-1"></div>
	{/each}
</div>
