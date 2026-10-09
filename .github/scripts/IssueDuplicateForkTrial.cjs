const crypto = require('node:crypto');
const { gather, validate } = require('./IssueDuplicates.cjs');

const sourceIssues = new Set([39280, 39270, 39263, 39169, 39132]);
const publicationIssueNumber = 946;
const publicationBodyHash = '88951e56e5a7abfe55e47dbe5c192cc1177ea6661c36ea064b8c823c16222bf5';
const workflowMarker = '<!-- gh-aw-workflow-call-id: kubaflo/maui/daily-repo-status -->';

async function assertPublicationTarget(github, context, issueNumber) {
    const { data: repository } = await github.rest.repos.get(context.repo);
    if (
        repository.full_name !== 'kubaflo/maui' ||
        repository.private !== false ||
        repository.visibility !== 'public'
    ) {
        throw new Error('Fork publication requires public kubaflo/maui.');
    }
    const { data: issue } = await github.rest.issues.get({
        ...context.repo,
        issue_number: publicationIssueNumber,
    });
    const bodyHash = crypto
        .createHash('sha256')
        .update(issue.body ?? '')
        .digest('hex');
    if (
        issue.id !== 5785547454 ||
        issue.number !== publicationIssueNumber ||
        issue.pull_request ||
        issue.user?.login !== 'kubaflo' ||
        issue.state !== 'open' ||
        issue.locked ||
        !Number.isInteger(issue.comments) ||
        issue.comments < 0 ||
        issue.comments >= sourceIssues.size ||
        issue.title !==
            '[Duplicate detector five-issue trial] Android, iOS, Windows and XAML reports' ||
        bodyHash !== publicationBodyHash ||
        issue.labels.some((label) => /^s\/duplicate\b/.test(label.name))
    ) {
        throw new Error('The fixed fork publication target changed or is no longer eligible.');
    }
    const reportedSources = new Set();
    let commentCount = 0;
    for await (const { data: comments } of github.paginate.iterator(
        github.rest.issues.listComments,
        { ...context.repo, issue_number: publicationIssueNumber, per_page: 100 },
    )) {
        for (const comment of comments) {
            commentCount++;
            const body = comment.body ?? '';
            const source = body.match(
                /^\s*<img alt="Issue (\d+)" src="https:\/\/img\.shields\.io\/badge\/Issue-\1-1f6feb\?labelColor=30363d(?:&|&amp;)style=flat-square">$/m,
            );
            const provenance = body.match(
                /^<!-- gh-aw-agentic-workflow: Issue duplicate detector fork trial, engine: copilot, version: 1\.0\.87, model: gpt-6\.1-sol, id: ([1-9]\d*), workflow_id: daily-repo-status, run: https:\/\/github\.com\/kubaflo\/maui\/actions\/runs\/\1 -->$/m,
            );
            const reportedSource = Number(source?.[1]);
            if (
                commentCount >= sourceIssues.size ||
                comment.user?.login !== 'github-actions[bot]' ||
                comment.user?.type !== 'Bot' ||
                !body.includes('<!-- Issue Duplicate Detector -->') ||
                !body.includes(workflowMarker) ||
                !provenance ||
                !sourceIssues.has(reportedSource) ||
                reportedSources.has(reportedSource) ||
                reportedSource === issueNumber
            ) {
                throw new Error('The fork batch contains an unexpected or repeated report.');
            }
            const runId = Number(provenance[1]);
            if (!Number.isSafeInteger(runId)) {
                throw new Error('The previous fork report has an invalid workflow run ID.');
            }
            // Public run metadata needs no additional publisher-token permission.
            const response = await fetch(
                `https://api.github.com/repos/kubaflo/maui/actions/runs/${runId}`,
                {
                    headers: {
                        Accept: 'application/vnd.github+json',
                        'User-Agent': 'maui-duplicate-fork-trial',
                        'X-GitHub-Api-Version': '2022-11-28',
                    },
                    signal: AbortSignal.timeout(15000),
                },
            );
            if (!response.ok) {
                throw new Error(
                    `Cannot verify previous fork run ${runId}: HTTP ${response.status}.`,
                );
            }
            const run = await response.json();
            if (
                run.id !== runId ||
                run.repository?.full_name !== 'kubaflo/maui' ||
                run.repository?.private !== false ||
                run.workflow_id !== 237748720 ||
                run.event !== 'workflow_dispatch' ||
                run.actor?.login !== 'kubaflo' ||
                run.triggering_actor?.login !== 'kubaflo' ||
                run.head_branch !== 'duplicate-detector-fork-trial-20261008' ||
                run.head_sha !== context.sha
            ) {
                throw new Error('The previous fork report is not from this trusted trial commit.');
            }
            reportedSources.add(reportedSource);
        }
    }
    if (commentCount !== issue.comments) {
        throw new Error('Fork batch comments changed during validation; request a fresh run.');
    }
}

async function getSourceContext(github, context, issueNumber) {
    if (
        context.repo.owner !== 'kubaflo' ||
        context.repo.repo !== 'maui' ||
        context.payload.repository.full_name !== 'kubaflo/maui' ||
        context.payload.repository.private !== false ||
        context.actor !== 'kubaflo' ||
        context.ref !== 'refs/heads/duplicate-detector-fork-trial-20261008' ||
        context.eventName !== 'workflow_dispatch' ||
        !['true', 'false', true, false].includes(context.payload.inputs?.staged) ||
        !sourceIssues.has(issueNumber) ||
        Number(context.payload.inputs?.issue_number) !== issueNumber ||
        (context.payload.inputs?.aw_context ?? '') !== ''
    ) {
        throw new Error('Untrusted fork trial context.');
    }
    const repository = { owner: 'dotnet', repo: 'maui' };
    const { data } = await github.rest.repos.get(repository);
    if (
        data.full_name !== 'dotnet/maui' ||
        data.private !== false ||
        data.visibility !== 'public'
    ) {
        throw new Error('Fork trial evidence must come from public dotnet/maui.');
    }
    // Only the evidence context is rebound; execution remains on the checked fork/ref.
    return {
        ...context,
        repo: repository,
        ref: `refs/heads/${data.default_branch}`,
        payload: { ...context.payload, repository: data },
    };
}

module.exports = {
    async gather(options) {
        const context = await getSourceContext(
            options.github,
            options.context,
            options.issueNumber,
        );
        await assertPublicationTarget(options.github, options.context, options.issueNumber);
        await gather({ ...options, context });
    },
    async validate(options) {
        const requestedStaging = ['true', true].includes(options.context.payload.inputs?.staged);
        if (options.staged !== requestedStaging) {
            throw new Error('The staging flag does not match the trusted dispatch.');
        }
        const context = await getSourceContext(
            options.github,
            options.context,
            options.issueNumber,
        );
        await assertPublicationTarget(options.github, options.context, options.issueNumber);
        await validate({
            ...options,
            context,
            reportRepository: options.context.repo,
            commentIssueNumber: publicationIssueNumber,
            compactReport: true,
        });
        await assertPublicationTarget(options.github, options.context, options.issueNumber);
    },
};
